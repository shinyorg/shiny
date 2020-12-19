using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Samples.SqliteGenerator;
using SQLite;
using ZNetCS.AspNetCore.ResumingFileResults.Extensions;
using F = System.IO.File;


namespace Samples.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BlobsController : Controller
    {
        [HttpGet("generate/{name}/{rows}/")]
        public async Task<IActionResult> Generate(string name, int rows)
        {
            await Generator.CreateSqlite(name, rows);
            return this.Ok();
        }


        [HttpGet("download/{name}")]
        public IActionResult Download(string name)
            => this.ResumingFile(F.OpenRead(name), "application/octet-stream");


        [Authorize]
        [HttpGet("downloadwithauth/{name}")]
        public IActionResult DownloadWithAuth(string name)
            => this.ResumingFile(F.OpenRead(name), "application/octet-stream");


        [HttpGet("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (F.Exists(file.FileName))
                F.Delete(file.FileName);

            await file.CopyToAsync(F.OpenWrite(file.FileName));
            if (Path.GetExtension(file.FileName) == "db")
            {
                using (var conn = new SQLiteConnection(file.FileName))
                {
                    conn.Execute("SELECT COUNT(*) FROM sqlite_master WHERE type='table'");
                }
            }
            // TODO: with resume offset
            return this.Ok();
        }
    }
}
