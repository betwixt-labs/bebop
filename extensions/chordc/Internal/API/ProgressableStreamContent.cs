using System.Net;
namespace Chord.Compiler.Internal.API;

internal sealed class ProgressableStreamContent : HttpContent
{
    private readonly Stream _stream;
    private readonly Action<long, long> _progressAction;
    private readonly CancellationToken _cancellationToken;

    public ProgressableStreamContent(Stream stream, Action<long, long> progressAction, CancellationToken cancellationToken)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _progressAction = progressAction ?? throw new ArgumentNullException(nameof(progressAction));
        _cancellationToken = cancellationToken;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        const int bufferSize = 4096;
        var buffer = new byte[bufferSize];
        long uploadedBytes = 0;
        int bytesRead;

        while ((bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cancellationToken)) != 0)
        {
            await stream.WriteAsync(buffer.AsMemory(0, bytesRead), _cancellationToken);
            uploadedBytes += bytesRead;
            _progressAction(uploadedBytes, _stream.Length);
            _cancellationToken.ThrowIfCancellationRequested();
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _stream.Length;
        return true;
    }
}