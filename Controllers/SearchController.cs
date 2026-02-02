using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTU.Models;
using MTU.Services;

namespace MTU.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IPostService _postService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            IAuthService authService,
            IPostService postService,
            ILogger<SearchController> logger)
        {
            _authService = authService;
            _postService = postService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string q)
        {
            var model = new SearchViewModel
            {
                Query = q ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(q))
            {
                return View(model);
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    ViewBag.CurrentUserId = userId;
                    
                    // Run search tasks in parallel
                    var peopleTask = _authService.SearchUsersAsync(q, 20);
                    var postsTask = _postService.SearchPostsAsync(q, userId, 20);

                    await Task.WhenAll(peopleTask, postsTask);

                    model.People = await peopleTask;
                    model.Posts = await postsTask;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing search for query: {Query}", q);
            }

            return View(model);
        }
    }
}
