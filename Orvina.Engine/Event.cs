namespace Orvina.Engine
{
    internal abstract class Event
    {
        public EventTypes EventType { get; private set; }

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

    internal class OnErrorEvent : Event
    {
        public OnErrorEvent(string error) : base(Event.EventTypes.OnError)
        {
            this.Error = error;
        }

        public string Error { get; private set; }
    }

    internal class OnFileFoundEvent : Event
    {
        public OnFileFoundEvent(string file, List<string> lines) : base(Event.EventTypes.OnFileFound)
        {
            this.File = file;
            this.Lines = lines;
        }

        public string File { get; private set; }
        public List<string> Lines { get; private set; }
    }

    internal class OnProgressEvent : Event
    {
        public OnProgressEvent(string file) : base(Event.EventTypes.OnProgress)
        {
            this.File = file;
        }

        public string File { get; private set; }
    }

    internal class OnSearchCompleteEvent : Event
    {
        public OnSearchCompleteEvent() : base(Event.EventTypes.OnSearchComplete)
        {
        }
    }
}