using HiddenValley.Core;
using Xunit;

namespace HiddenValley.Tests
{
    public class SmokeTests
    {
        [Fact]
        public void Clock_reports_phases_from_settings()
        {
            var clock = new Clock(new ClockSettings(), 0, 420); // 07:00, before day_start 08:00
            Assert.Equal("dawn", clock.PhaseName);

            clock.Advance(60);
            Assert.Equal("day", clock.PhaseName);

            clock.Advance(60 * 20);
            Assert.Equal(1, clock.Day);
        }

        [Fact]
        public void AdvanceToPhase_always_moves_forward()
        {
            var clock = new Clock(new ClockSettings(), 0, 300); // exactly dawn
            clock.AdvanceToPhase("dawn");

            Assert.Equal(1, clock.Day);
            Assert.Equal("dawn", clock.PhaseName);
        }
    }
}
