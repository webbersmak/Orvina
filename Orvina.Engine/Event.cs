namespace Orvina.Engine
{
    internal abstract class Event
    {
        public readonly EventTypes EventType;

        public Event(EventTypes eventType)
        {
            this.EventType = eventType;
        }

        public enum EventTypes
        {
            OnError,
            OnFileFound,
            OnProgress,
            OnSearchComplete
        }
    }

    internal sealed class OnErrorEvent : Event
    {
        public OnErrorEvent(string error) : base(Event.EventTypes.OnError)
        {
            this.Error = error;
        }

        public readonly string Error;
    }

    internal sealed class OnFileFoundEvent : Event
    {
        public OnFileFoundEvent(string file, string[] lines) : base(Event.EventTypes.OnFileFound)
        {
            this.File = file;
            this.Lines = lines;
        }

        public readonly string File;
        public readonly string[] Lines;
    }

    internal sealed class OnProgressEvent : Event
    {
        public OnProgressEvent(string file) : base(Event.EventTypes.OnProgress)
        {
            this.File = file;
        }

        public readonly string File;
    }

    internal sealed class OnSearchCompleteEvent : Event
    {
        public OnSearchCompleteEvent() : base(Event.EventTypes.OnSearchComplete)
        {
        }
    }
}