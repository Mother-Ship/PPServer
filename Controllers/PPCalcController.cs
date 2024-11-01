using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<string, object> fileLocks = new ConcurrentDictionary<string, object>();


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
            // 获取或添加锁对象，基于文件名
            var fileLock = fileLocks.GetOrAdd(save, new object());

            lock (fileLock)  // 针对特定文件的锁
            {
                // 再次检查文件是否存在，避免重复下载
                if (!System.IO.File.Exists(save) || req.refresh)
                {
                    var url = "http://osu.ppy.sh/osu/" + req.bid;

                    using (var web = new WebClient())
                    {
                        web.DownloadFile(url, save);
                    }
                }
            }

            // 移除已完成下载的文件锁
            fileLocks.TryRemove(save, out _);

            return calcService.calc(req.bid + ".osu", req.userScore);
        }
    }
}