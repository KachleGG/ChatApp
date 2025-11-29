using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class Program
{
    static void Main(string[] args) {
        var _ = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
