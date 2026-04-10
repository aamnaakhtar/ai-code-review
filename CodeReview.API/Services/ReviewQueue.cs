using System.Threading.Channels;
using CodeReview.API.Models.Entities;

namespace CodeReview.API.Services;

public class ReviewQueue
{
    // Channel is .NET's built-in thread-safe queue
    // Capacity 100 = max 100 pending jobs before backpressure kicks in
    private readonly Channel<ReviewJob> _channel =
        Channel.CreateBounded<ReviewJob>(100);

    // Writer — used to add jobs to the queue
    public ChannelWriter<ReviewJob> Writer => _channel.Writer;

    // Reader — used by the background worker to consume jobs
    public ChannelReader<ReviewJob> Reader => _channel.Reader;
}