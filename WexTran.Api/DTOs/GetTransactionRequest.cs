using System.ComponentModel.DataAnnotations;

namespace WexTran.Api.DTOs
{
    public class GetTransactionRequest
    {
        [Required(ErrorMessage = "Currency is required.")]
        public string Currency { get; set; }
    }
}
