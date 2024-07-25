using System;
using chartApp.models;
using chartApp.service;
using Microsoft.AspNetCore.Mvc;

namespace chartApp.Controllers
{
    [Route("/api/")]
    [ApiController]
	public class ChartController : ControllerBase
	{
        private readonly DbService _dbService;

        public ChartController(DbService dbService)
        {
            _dbService = dbService;
        }

        [HttpPost("getData")]
        public ActionResult<ChartData> GetChartData([FromBody] DbRequest dbRequest)
        {
            try
            {
                var data = _dbService.GetData(dbRequest);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}

