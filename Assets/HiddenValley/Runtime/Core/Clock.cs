namespace HiddenValley.Core
{
    /// <summary>
    /// Time of day. Phase boundaries are authored in settings.json, not hardcoded, so the
    /// "flow is only sufficient at dawn" puzzle step can be retuned without a rebuild.
    /// </summary>
    public sealed class Clock
    {
        private readonly ClockSettings _settings;

        public int Day { get; private set; }
        public int Minute { get; private set; }

        public Clock(ClockSettings settings, int day = 0, int minute = -1)
        {
            _settings = settings ?? new ClockSettings();
            Day = day;
            Minute = minute < 0 ? _settings.StartMinute : minute;
            Normalise();
        }

        public ClockSettings Settings => _settings;

        public string PhaseName
        {
            get
            {
                if (Minute >= _settings.NightStart || Minute < _settings.DawnStart) return "night";
                if (Minute < _settings.DayStart) return "dawn";
                if (Minute < _settings.DuskStart) return "day";
                return "dusk";
            }
        }

        public void Advance(int minutes)
        {
            if (minutes <= 0) return;
            Minute += minutes;
            Normalise();
        }

        /// <summary>
        /// Moves forward to the next time the named phase starts. Always moves forward —
        /// sleeping to dawn when it is already dawn costs a full day, which is the
        /// behaviour players expect and the one that avoids a zero-length wait loop.
        /// </summary>
        public void AdvanceToPhase(string phase)
        {
            int target = PhaseStart(phase);
            if (target < 0) return;

            int delta = target - Minute;
            if (delta <= 0) delta += _settings.DayLengthMinutes;
            Advance(delta);
        }

        public int PhaseStart(string phase)
        {
            switch (phase)
            {
                case "dawn": return _settings.DawnStart;
                case "day": return _settings.DayStart;
                case "dusk": return _settings.DuskStart;
                case "night": return _settings.NightStart;
                default: return -1;
            }
        }

        /// <summary>Fraction through the day, for driving a light rig or a sky.</summary>
        public float NormalisedTime => (float)Minute / _settings.DayLengthMinutes;

        private void Normalise()
        {
            int len = _settings.DayLengthMinutes;
            if (len <= 0) return;

            while (Minute >= len)
            {
                Minute -= len;
                Day++;
            }
            while (Minute < 0)
            {
                Minute += len;
                Day--;
            }
        }

        public override string ToString()
        {
            int h = Minute / 60;
            int m = Minute % 60;
            return $"Day {Day} {h:00}:{m:00} ({PhaseName})";
        }
    }
}
