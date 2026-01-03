using System.Threading.Tasks;

namespace GameAI.MiddleWares
{
    public class loggingMiddleWare
    {
        private readonly RequestDelegate next; // call next middile ware
        private readonly ILogger logger;

        public loggingMiddleWare(RequestDelegate next, ILogger<loggingMiddleWare> logger)
        {
            this.next = next;
            this.logger = logger;
        }
        public async Task Invoke(HttpContext context, ILogger<loggingMiddleWare> logger)
        {
            // request 
            logger.LogCritical("start at " + DateTime.Now + " " + context.Request.Path + " " + context.Request.Method);

            await next(context);

            // request end
            logger.LogInformation("start at " + DateTime.Now + " " + context.Request.Path + " " + context.Request.Method);



        }
    }
}
