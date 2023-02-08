using System.Collections.Generic;

namespace AriBotV4.Models
{
    public class QnAMaker
    {
        public List<Answers> Answers { get; set; }
    }
    public class Answers
    {
        public List<string> Questions { get; set; }
        public string Answer { get; set; }
        public double Score { get; set; }
        public int Id { get; set; }
        public string Source { get; set; }
        public Context context { get; set; }

        public List<Metadata> Metadata { get; set; }
    }
    public class Context
    {
        public bool isContextOnly { get; set; }
        public List<Prompt> prompts { get; set; }
    }
    public class Prompt
    {
        public int displayOrder { get; set; }
        public int qnaId { get; set; }
        public string displayText { get; set; }
    }
    public class Metadata
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }   
}