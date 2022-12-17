using Microsoft.AspNetCore.Mvc;
using ProductApi.Model;

namespace ProductApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private static DateTime _recoveryTime = DateTime.UtcNow;
        private static readonly Random Random = new Random();

        public ProductController()
        {

        }

        [HttpGet]
        [Route("GetProductDetails")]
        public async Task<IActionResult> GetProductDetails()
        {
            try
            {
                if (_recoveryTime > DateTime.UtcNow)
                {
                    throw new Exception("Error GetProductDetails !!!");
                }

                if (_recoveryTime < DateTime.UtcNow && Random.Next(1, 4) == 1)
                {
                    _recoveryTime = DateTime.UtcNow.AddSeconds(30);
                }

                return Ok(new Products() { Id = Random.Next(1, 100), Name = Guid.NewGuid().ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(503);
            }
        }

        [HttpGet]
        [Route("GetFallBack1")]
        public async Task<IActionResult> GetFallBack1()
        {
            return Ok("Success");
        }

        [HttpGet]
        [Route("GetFallBack2")]
        public async Task<IActionResult> GetFallBack2()
        {
            return NotFound("NotFound");
        }

        [HttpGet]
        [Route("GetTimeout")]
        public async Task<IActionResult> GetTimeout()
        {
            Thread.Sleep(20000);

            return Ok(new Products() { Id = Random.Next(1, 100), Name = Guid.NewGuid().ToString() });
        }
    }
}