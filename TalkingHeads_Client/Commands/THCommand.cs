namespace TalkingHeads.Commands
{
    interface THCommand
    {
        public Task<string> Execute(string command);
    }
}