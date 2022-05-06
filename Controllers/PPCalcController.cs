using System.Net;
using Microsoft.AspNetCore.Mvc;
using PPServer.models;
using PPServer.models.request;
using PPServer.models.result;
using PPServer.Services;

namespace PPServer.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class PPCalcController : ControllerBase
    {
        private readonly ILogger<PPCalcController> _logger;
        private readonly CalcService calcService = new CalcService();

        public PPCalcController(ILogger<PPCalcController> logger)
        {
            _logger = logger;
        }


        [HttpPost(Name = "~/calculate")]
        [Consumes("application/json")]
        public CalcResult Calculate(CalculateRequest req)
        {
            System.IO.File.WriteAllText(req.bid + ".osu", req.osuFile);
            return calcService.calc(req.bid + ".osu", req.userScore);
        }

        [HttpPost(Name = "~/calculateById")]
        [Consumes("application/json")]
        public CalcResult CalculateByBeatmapId(CalculateByBidRequest req)
        {
            var save = req.bid + ".osu";
            if (!System.IO.File.Exists(save) || req.refresh)
            {
                var url = "http://osu.ppy.sh/osu/" + req.bid;

                using (var web = new WebClient())
                {
                    web.DownloadFile(url, save);
                }
            }

            return calcService.calc(req.bid + ".osu", req.userScore);
        }
    }
}