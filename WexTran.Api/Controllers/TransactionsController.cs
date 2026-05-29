using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WexTran.Api.DTOs;
using WexTran.Api.Services;

namespace WexTran.Api.Controllers
{
    /// <summary>Purchase transaction management.</summary>
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

        /// <summary>Store a new purchase transaction.</summary>
        /// <remarks>
        /// The purchase amount is stored in US dollars, rounded to the nearest cent.
        /// A unique identifier is assigned to the transaction upon creation.
        /// </remarks>
        /// <param name="request">Transaction details to store.</param>
        /// <returns>The stored transaction including its assigned identifier.</returns>
        /// <response code="200">Transaction stored successfully.</response>
        /// <response code="400">Request is invalid (e.g. description exceeds 50 characters, amount is not positive).</response>
        [HttpPost]
        [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TransactionResponse>> Create([FromBody] CreateTransactionRequest request)
        {
            var result = await _transactionService.CreateAsync(request);
            return Ok(result);
        }

        /// <summary>Retrieve a purchase transaction converted to a specified currency.</summary>
        /// <remarks>
        /// Converts the stored USD amount using the Treasury Reporting Rates of Exchange.
        /// The rate used is the most recent rate that is on or before the transaction date
        /// and within the last 6 months. If no qualifying rate exists, the request is rejected.
        ///
        /// The <paramref name="trnid"/> must be the identifier returned when the transaction was created.
        ///
        /// The <c>currency</c> query parameter must match the <c>country_currency_desc</c> value
        /// from the Treasury dataset, for example: <c>Canada-Dollar</c>, <c>Mexico-Peso</c>,
        /// <c>Euro Zone-Euro</c>.
        /// </remarks>
        /// <param name="trnid">The unique identifier of the stored transaction.</param>
        /// <param name="request">Query parameters including the target currency.</param>
        /// <returns>The transaction with the exchange rate and converted amount.</returns>
        /// <response code="200">Transaction retrieved and converted successfully.</response>
        /// <response code="404">No transaction found for the given identifier.</response>
        /// <response code="422">No qualifying exchange rate is available within 6 months of the transaction date.</response>
        [HttpGet("{trnid}")]
        [ProducesResponseType(typeof(TransactionByCurrencyResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<TransactionByCurrencyResponse>> GetById(Guid trnid, [FromQuery] GetTransactionRequest request)
        {
            var result = await _transactionService.GetByCurrencyAsync(trnid, request.Currency);
            return Ok(result);
        }
    }
}
