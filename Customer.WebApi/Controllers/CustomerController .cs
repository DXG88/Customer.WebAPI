using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;

namespace Customer.WebApi.Controllers
{
    public class CustomerController : ControllerBase
    {
        private readonly ScoreUpdateService _scoreUpdateService;
        public CustomerController(ScoreUpdateService scoreUpdateService)
        {
            this._scoreUpdateService = scoreUpdateService;
        }

        [HttpPost("{customerid}/score/{score}")]
        public IActionResult UpdateScore(long customerid, decimal score)
        {
            if (score < -1000 || score > 1000)
                return BadRequest("Score must be in range of [-1000, +1000].");

            _scoreUpdateService.EnqueueScoreUpdate(customerid, score);

            return Ok();
        }

        [HttpGet("leaderboard")]
        public IActionResult GetCustomersByRank(int start, int end)
        {
            var result = _scoreUpdateService.GetCustomersByRank(start, end)
                .Select(c => new { CustomerId = c.Item1, Score = c.Item2, Rank = c.Item3 });
            return Ok(result);
        }

        [HttpGet("leaderboard/{customerid}")]
        public IActionResult GetCustomerById(long customerid, int high = 0, int low = 0)
        {
            var result = _scoreUpdateService.GetCustomerById(customerid, high, low)
                .Select(c => new { CustomerId = c.Item1, Score = c.Item2, Rank = c.Item3 });

            return Ok(result);
        }
    }
}
