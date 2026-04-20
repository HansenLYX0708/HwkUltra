namespace HWKUltra.AI.Abstractions
{
    public enum LLMMessageRole
    {
        System,
        User,
        Assistant
    }

    public class LLMMessage
    {
        public LLMMessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;

        public LLMMessage() { }

        public LLMMessage(LLMMessageRole role, string content)
        {
            Role = role;
            Content = content;
        }

        public string RoleString => Role switch
        {
            LLMMessageRole.System => "system",
            LLMMessageRole.User => "user",
            LLMMessageRole.Assistant => "assistant",
            _ => "user"
        };
    }
}
