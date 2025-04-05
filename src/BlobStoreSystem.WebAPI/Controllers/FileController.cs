using BlobStoreSystem.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlobStoreSystem.WebAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class FileController : ControllerBase
{
    private readonly IFsProvider _fsProvider;
    private readonly IWebHostEnvironment _env;

    public FileController(IFsProvider fsProvider, IWebHostEnvironment env)
    {
        _fsProvider = fsProvider;
        _env = env;
    }

    // POST: FileController/upload
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromQuery] string path, IFormFile file)
    {
        //if (file == null || file.Length == 0)
        if (file == null)
            return BadRequest("No file uploaded.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        await _fsProvider.WriteFileAsync(path, bytes);

        return Ok(new { message = "File uploaded", path });
    }

    // GET: FileController/download
    [HttpGet("download")]
    public async Task<IActionResult> DownloadFile([FromQuery] string path)
    {
        var fileBytes = await _fsProvider.ReadFileAsync(path);
        var fileInfo = await _fsProvider.GetInfoAsync(path);
        if (fileInfo == null || fileInfo.IsDirectory)
            return NotFound("File not found.");

        var mimeType = fileInfo.MimeType ?? "application/octet-stream";
        var fileName = Path.GetFileName(path);

        return File(fileBytes, mimeType, fileName);
        //var fileBytes = await _fsProvider.ReadFileAsync(path);
        //var contentType = "application/octet-stream";
        //var fileName = Path.GetFileName(path);

        //return File(fileBytes, contentType, fileName);
    }

    // DELETE: FileController/delete
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteFile([FromQuery] string path)
    {
        await _fsProvider.DeleteFileAsync(path);

        return Ok(new { message = "File deleted", path });
    }

    // POST: FileController/copy
    [HttpPost("copy")]
    public async Task<IActionResult> CopyFile([FromQuery] string oldPath, [FromQuery] string newPath)
    {
        await _fsProvider.CopyFileAsync(oldPath, newPath);

        return Ok(new { message = "File copied", oldPath, newPath });
    }

    // POST: FileController/move
    [HttpPost("move")]
    public async Task<IActionResult> MoveFile([FromQuery] string oldPath, [FromQuery] string newPath)
    {
        await _fsProvider.MoveFileAsync(oldPath, newPath);
        
        return Ok(new { message = "File moved", oldPath, newPath });
    }

    // POST: FileController/rename
    [HttpPost("rename")]
    public async Task<IActionResult> RenameFile([FromQuery] string oldPath, [FromQuery] string newName)
    {
        try
        {
            await _fsProvider.RenameFileAsync(oldPath, newName);
            return Ok("File renamed successfully.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}