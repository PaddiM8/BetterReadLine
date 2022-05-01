using System;
using System.Linq;
using Xunit;

using static BetterReadLine.ReadLine;

namespace BetterReadLine.Tests
{
    public class ReadLineTests : IDisposable
    {
        private ReadLine _readLine = new();
        
        public ReadLineTests()
        {
            string[] history = new string[] { "ls -a", "dotnet run", "git init" };
            _readLine.AddHistory(history);
        }

        [Fact]
        public void TestNoInitialHistory() 
        {
            Assert.Equal(3, _readLine.GetHistory().Count);
        }

        [Fact]
        public void TestUpdatesHistory() 
        {
            _readLine.AddHistory("mkdir");
            Assert.Equal(4, _readLine.GetHistory().Count);
            Assert.Equal("mkdir", _readLine.GetHistory().Last());
        }

        [Fact]
        public void TestGetCorrectHistory() 
        {
            Assert.Equal("ls -a", _readLine.GetHistory()[0]);
            Assert.Equal("dotnet run", _readLine.GetHistory()[1]);
            Assert.Equal("git init", _readLine.GetHistory()[2]);
        }

        public void Dispose()
        {
            // If all above tests pass
            // clear history works
            _readLine.ClearHistory();
        }
    }
}
