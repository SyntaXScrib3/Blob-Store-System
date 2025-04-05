using BlobStoreSystem.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlobStoreSystem.WebAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DirectoryController : ControllerBase
{
    private readonly IFsProvider _fsProvider;

    public DirectoryController(IFsProvider fsProvider)
    {
        _fsProvider = fsProvider;
    }

    // POST: DirectoryController/create
    [HttpPost("create")]
    public async Task<IActionResult> CreateDirectory([FromQuery] string path)
    {
        await _fsProvider.CreateDirectoryAsync(path);
        return Ok(new { message = "Directory created", path });
    }

    // DELETE: DirectoryController/delete
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteDirectory([FromQuery] string path)
    {
        await _fsProvider.DeleteDirectoryAsync(path);
        return Ok(new { message = "Directory deleted", path });
    }

    // GET: DirectoryController/list
    [HttpGet("list")]
    public async Task<IActionResult> ListDirectory([FromQuery] string path)
    {
        var nodes = await _fsProvider.ListDirectoryAsync(path);
        return Ok(nodes);
    }

    // GET: DirectoryController/move
    [HttpPost("move")]
    public async Task<IActionResult> MoveDirectory([FromQuery] string oldPath, [FromQuery] string newPath)
    {
        await _fsProvider.MoveDirectoryAsync(oldPath, newPath);
        return Ok(new { message = "Directory moved", oldPath, newPath });
    }

    // POST: DirectoryController/copy
    [HttpPost("copy")]
    public async Task<IActionResult> CopyDirectory([FromQuery] string oldPath, [FromQuery] string newPath)
    {
        await _fsProvider.CopyDirectoryAsync(oldPath, newPath);
        return Ok(new { message = "Directory copied", oldPath, newPath });
    }

    // POST: DirectoryController/rename
    [HttpPost("rename")]
    public async Task<IActionResult> RenameDirectory([FromQuery] string oldPath, [FromQuery] string newName)
    {
        try
        {
            await _fsProvider.RenameDirectoryAsync(oldPath, newName);
            return Ok("Directory renamed successfully.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}