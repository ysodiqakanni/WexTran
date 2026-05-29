using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WexTran.Api.DTOs;
using WexTran.Api.Services;

namespace WexTran.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(ITransactionService transactionService, ILogger<TransactionsController> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<TransactionResponse>> Create([FromBody] CreateTransactionRequest request)
        {
            var result = await _transactionService.CreateAsync(request);
            return Ok(result);
        }

        [HttpGet("{trnid}")]
        public async Task<ActionResult<TransactionByCurrencyResponse>> GetById(Guid trnid, [FromQuery] GetTransactionRequest request)
        {
            var result = await _transactionService.GetByCurrencyAsync(trnid, request.Currency);
            return Ok(result);
        }
    }
}
