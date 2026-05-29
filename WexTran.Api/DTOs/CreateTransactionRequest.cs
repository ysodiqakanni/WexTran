using System;
using System.ComponentModel.DataAnnotations;

namespace WexTran.Api.DTOs
{
    public class CreateTransactionRequest
    {
        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(50, ErrorMessage = "Description must not exceed 50 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Transaction date is required.")]
        public DateTime TransactionDate { get; set; }

        [Range(0.01, 10000000, ErrorMessage = "Purchase amount must be a positive value.")]
        public decimal AmountUsd { get; set; }
    }
}
