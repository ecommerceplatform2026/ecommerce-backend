using Application.Common.Response;
using Application.Common.Validations;
using Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Delivery
{
    [DateRangeValidation(nameof(CreatedFrom), nameof(CreatedTo))]
    public class GetShipmentRequest : PagingRequest
    {
        public DeliveryStatus? Status { get; set; }
        public Guid? OrderId { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
