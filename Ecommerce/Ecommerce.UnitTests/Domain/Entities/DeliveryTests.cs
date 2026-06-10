using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using System;
using System.Reflection;

namespace Ecommerce.UnitTests.EntityTests
{
    public class DeliveryTests
    {
        [Fact]
        public void Create_WithValidData_SetsPropertiesAndStatusPending()
        {
            var orderId = Guid.NewGuid();

            var delivery = Delivery.Create(
                orderId, "GHN",
                "Receiver", "0900000000", "123 Street",
                "Province", "District", "Ward",
                500, 10, 10, 10,
                100_000, 150_000,
                "Order #100001");

            delivery.OrderId.Should().Be(orderId);
            delivery.CarrierCode.Should().Be("GHN");
            delivery.ToName.Should().Be("Receiver");
            delivery.ToPhone.Should().Be("0900000000");
            delivery.ToAddress.Should().Be("123 Street");
            delivery.Province.Should().Be("Province");
            delivery.District.Should().Be("District");
            delivery.Ward.Should().Be("Ward");
            delivery.Weight.Should().Be(500);
            delivery.Length.Should().Be(10);
            delivery.Width.Should().Be(10);
            delivery.Height.Should().Be(10);
            delivery.CodAmount.Should().Be(100_000);
            delivery.InsuranceValue.Should().Be(150_000);
            delivery.Note.Should().Be("Order #100001");
            delivery.Status.Should().Be(DeliveryStatus.Pending);
            delivery.TrackingCode.Should().BeEmpty();
            delivery.CarrierOrderCode.Should().BeNull();
            delivery.ShippingFee.Should().Be(0);
        }

        [Fact]
        public void Create_WithEmptyOrderId_ThrowsArgumentException()
        {
            Action act = () => Delivery.Create(
                Guid.Empty, "GHN",
                "Receiver", "0900000000", "123 Street",
                "Province", "District", "Ward",
                500, 10, 10, 10,
                0, 0, null);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*Order ID cannot be empty.*");
        }

        [Fact]
        public void Create_WithEmptyCarrierCode_ThrowsArgumentException()
        {
            Action act = () => Delivery.Create(
                Guid.NewGuid(), "",
                "Receiver", "0900000000", "123 Street",
                "Province", "District", "Ward",
                500, 10, 10, 10,
                0, 0, null);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*Carrier code cannot be empty.*");
        }

        [Fact]
        public void MarkShipmentCreated_WhenPending_SetsTrackingAndStatusCreated()
        {
            var delivery = CreatePendingDelivery();

            delivery.MarkShipmentCreated("TRACK-123", "GHN-ORDER-456", 35_000);

            delivery.TrackingCode.Should().Be("TRACK-123");
            delivery.CarrierOrderCode.Should().Be("GHN-ORDER-456");
            delivery.ShippingFee.Should().Be(35_000);
            delivery.Status.Should().Be(DeliveryStatus.Created);
        }

        [Fact]
        public void MarkShipmentCreated_WhenNotPending_ThrowsInvalidOperationException()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);

            Action act = () => delivery.MarkShipmentCreated("TRACK-2", "ORD-2", 20_000);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot mark shipment as created when status is Created.");
        }

        [Fact]
        public void MarkShipmentCreated_WithNullTrackingCode_ThrowsArgumentNullException()
        {
            var delivery = CreatePendingDelivery();

            Action act = () => delivery.MarkShipmentCreated(null!, "ORD-1", 10_000);

            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*trackingCode*");
        }

        [Fact]
        public void MarkFailed_SetsStatusToFailed()
        {
            var delivery = CreatePendingDelivery();

            delivery.MarkFailed("API timeout");

            delivery.Status.Should().Be(DeliveryStatus.Failed);
            delivery.Note.Should().Be("API timeout");
        }

        [Fact]
        public void MarkFailed_WorksFromCreatedStatus()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);

            delivery.MarkFailed("Carrier returned error");

            delivery.Status.Should().Be(DeliveryStatus.Failed);
            delivery.Note.Should().Be("Carrier returned error");
        }

        [Fact]
        public void MarkFailed_WithNullNote_DoesNotThrow()
        {
            var delivery = CreatePendingDelivery();

            delivery.MarkFailed(null);

            delivery.Status.Should().Be(DeliveryStatus.Failed);
            delivery.Note.Should().BeNull();
        }

        [Fact]
        public void MarkPickedUp_WhenCreated_SetsStatusPickedUp()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);

            delivery.MarkPickedUp();

            delivery.Status.Should().Be(DeliveryStatus.PickedUp);
        }

        [Fact]
        public void MarkPickedUp_FromAnyStatus_SetsStatusPickedUp()
        {
            var delivery = CreatePendingDelivery();

            delivery.MarkPickedUp();

            delivery.Status.Should().Be(DeliveryStatus.PickedUp);
        }

        [Fact]
        public void MarkInTransit_WhenPickedUp_SetsStatusInTransit()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);
            delivery.MarkPickedUp();

            delivery.MarkInTransit();

            delivery.Status.Should().Be(DeliveryStatus.InTransit);
        }

        [Fact]
        public void MarkInTransit_FromAnyStatus_SetsStatusInTransit()
        {
            var delivery = CreatePendingDelivery();

            delivery.MarkInTransit();

            delivery.Status.Should().Be(DeliveryStatus.InTransit);
        }

        [Fact]
        public void MarkOutForDelivery_WhenInTransit_SetsStatusOutForDelivery()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);
            delivery.MarkPickedUp();
            delivery.MarkInTransit();

            delivery.MarkOutForDelivery();

            delivery.Status.Should().Be(DeliveryStatus.OutForDelivery);
        }

        [Fact]
        public void MarkOutForDelivery_FromAnyStatus_SetsStatusOutForDelivery()
        {
            var delivery = CreatePendingDelivery();

            delivery.MarkOutForDelivery();

            delivery.Status.Should().Be(DeliveryStatus.OutForDelivery);
        }

        [Fact]
        public void MarkDelivered_FromOutForDelivery_SetsStatusDelivered()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);
            delivery.MarkPickedUp();
            delivery.MarkInTransit();
            delivery.MarkOutForDelivery();

            delivery.MarkDelivered();

            delivery.Status.Should().Be(DeliveryStatus.Delivered);
        }

        [Fact]
        public void MarkDelivered_WhenAlreadyDelivered_IsIdempotent()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);
            delivery.MarkPickedUp();
            delivery.MarkInTransit();
            delivery.MarkOutForDelivery();
            delivery.MarkDelivered();

            delivery.MarkDelivered();

            delivery.Status.Should().Be(DeliveryStatus.Delivered);
        }

        [Fact]
        public void MarkDelivered_WhenCancelled_SetsStatusDelivered()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);
            delivery.MarkCancelled();

            delivery.MarkDelivered();

            delivery.Status.Should().Be(DeliveryStatus.Delivered);
        }

        [Fact]
        public void MarkDelivered_WhenReturned_SetsStatusDelivered()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);
            delivery.MarkReturned();

            delivery.MarkDelivered();

            delivery.Status.Should().Be(DeliveryStatus.Delivered);
        }

        [Fact]
        public void MarkDelivered_WhenException_SetsStatusDelivered()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);
            delivery.MarkException("error");

            delivery.MarkDelivered();

            delivery.Status.Should().Be(DeliveryStatus.Delivered);
        }

        [Fact]
        public void MarkCancelled_WhenNotDelivered_SetsStatusCancelled()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);

            delivery.MarkCancelled();

            delivery.Status.Should().Be(DeliveryStatus.Cancelled);
        }

        [Fact]
        public void MarkCancelled_FromAnyStatus_SetsStatusCancelled()
        {
            var delivery = CreatePendingDelivery();

            delivery.MarkCancelled();

            delivery.Status.Should().Be(DeliveryStatus.Cancelled);
        }

        [Fact]
        public void MarkReturned_SetsStatusReturned()
        {
            var delivery = CreatePendingDelivery();

            delivery.MarkReturned();

            delivery.Status.Should().Be(DeliveryStatus.Returned);
        }

        [Fact]
        public void MarkException_SetsStatusExceptionAndMessage()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);

            delivery.MarkException("Lost in transit");

            delivery.Status.Should().Be(DeliveryStatus.Exception);
            delivery.Note.Should().Be("Lost in transit");
        }

        [Fact]
        public void MarkException_WithNoMessage_DoesNotChangeNote()
        {
            var delivery = CreatePendingDelivery();
            delivery.MarkShipmentCreated("TRACK-1", "ORD-1", 10_000);
            var originalNote = delivery.Note;

            delivery.MarkException(null);

            delivery.Status.Should().Be(DeliveryStatus.Exception);
            delivery.Note.Should().Be(originalNote);
        }

        private static Delivery CreatePendingDelivery()
        {
            return Delivery.Create(
                Guid.NewGuid(), "GHN",
                "Receiver", "0900000000", "123 Street",
                "Province", "District", "Ward",
                500, 10, 10, 10,
                100_000, 150_000,
                "Order #100001");
        }
    }
}
