﻿using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json;
using StreamingClient.Base.Util;
using StreamingClient.Base.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace MixItUp.Base.Model.Commands.Games
{
    [DataContract]
    public class TriviaGameQuestionModel
    {
        [DataMember]
        public string Question { get; set; }
        [DataMember]
        public List<string> Answers { get; set; } = new List<string>();

        [JsonIgnore]
        public int TotalAnswers { get { return this.Answers.Count; } }

        [JsonIgnore]
        public string CorrectAnswer { get { return this.Answers[0]; } }

        public TriviaGameQuestionModel() { }

#pragma warning disable CS0612 // Type or member is obsolete
        internal TriviaGameQuestionModel(Base.Commands.TriviaGameQuestion question)
        {
            this.Question = question.Question;
            this.Answers.Add(question.CorrectAnswer);
            this.Answers.AddRange(question.WrongAnswers);
        }
#pragma warning disable CS0612 // Type or member is obsolete
    }

    [DataContract]
    public class TriviaGameCommandModel : GameCommandModelBase
    {
        public const string GameTriviaQuestionSpecialIdentifier = "gamequestion";
        public const string GameTriviaAnswersSpecialIdentifier = "gameanswers";
        public const string GameTriviaCorrectAnswerSpecialIdentifier = "gamecorrectanswer";

        private const string OpenTDBUrl = "https://opentdb.com/api.php?amount=1&type=multiple";

        private static readonly TriviaGameQuestionModel FallbackQuestion = new TriviaGameQuestionModel()
        {
            Question = "Looks like we failed to get a question, who's to blame?",
            Answers = new List<string>() { "SaviorXTanren", "SaviorXTanren", "SaviorXTanren", "SaviorXTanren" }
        };

        private class OpenTDBResults
        {
            public int response_code { get; set; }
            public List<OpenTDBQuestion> results { get; set; }
        }

        private class OpenTDBQuestion
        {
            public string category { get; set; }
            public string type { get; set; }
            public string difficulty { get; set; }
            public string question { get; set; }
            public string correct_answer { get; set; }
            public List<string> incorrect_answers { get; set; }
        }

        [DataMember]
        public int TimeLimit { get; set; }
        [DataMember]
        public bool UseRandomOnlineQuestions { get; set; }
        [DataMember]
        public int WinAmount { get; set; }

        [DataMember]
        public List<TriviaGameQuestionModel> CustomQuestions { get; set; } = new List<TriviaGameQuestionModel>();

        [DataMember]
        public CustomCommandModel StartedCommand { get; set; }
        [DataMember]
        public CustomCommandModel UserJoinCommand { get; set; }

        [DataMember]
        public CustomCommandModel CorrectAnswerCommand { get; set; }
        [DataMember]
        public CustomCommandModel UserSuccessCommand { get; set; }
        [DataMember]
        public CustomCommandModel UserFailureCommand { get; set; }

        [JsonIgnore]
        private CommandParametersModel runParameters;
        [JsonIgnore]
        private TriviaGameQuestionModel question;
        [JsonIgnore]
        private Dictionary<int, string> numbersToAnswers = new Dictionary<int, string>();
        [JsonIgnore]
        private Dictionary<UserViewModel, CommandParametersModel> runUsers = new Dictionary<UserViewModel, CommandParametersModel>();
        [JsonIgnore]
        private Dictionary<UserViewModel, int> runUserSelections = new Dictionary<UserViewModel, int>();

        public TriviaGameCommandModel(string name, HashSet<string> triggers, int timeLimit, bool useRandomOnlineQuestions, int winAmount, IEnumerable<TriviaGameQuestionModel> customQuestions,
            CustomCommandModel startedCommand, CustomCommandModel userJoinCommand, CustomCommandModel correctAnswerCommand, CustomCommandModel userSuccessCommand, CustomCommandModel userFailureCommand)
            : base(name, triggers, GameCommandTypeEnum.Trivia)
        {
            this.TimeLimit = timeLimit;
            this.UseRandomOnlineQuestions = useRandomOnlineQuestions;
            this.WinAmount = winAmount;
            this.CustomQuestions = new List<TriviaGameQuestionModel>(customQuestions);
            this.StartedCommand = startedCommand;
            this.UserJoinCommand = userJoinCommand;
            this.CorrectAnswerCommand = correctAnswerCommand;
            this.UserSuccessCommand = userSuccessCommand;
            this.UserFailureCommand = userFailureCommand;
        }

#pragma warning disable CS0612 // Type or member is obsolete
        internal TriviaGameCommandModel(Base.Commands.TriviaGameCommand command)
            : base(command, GameCommandTypeEnum.Trivia)
        {
            this.TimeLimit = command.TimeLimit;
            this.UseRandomOnlineQuestions = command.UseRandomOnlineQuestions;
            this.WinAmount = command.WinAmount;
            this.CustomQuestions = new List<TriviaGameQuestionModel>(command.CustomQuestions.Select(q => new TriviaGameQuestionModel(q)));
            this.StartedCommand = new CustomCommandModel(command.StartedCommand) { IsEmbedded = true };
            this.UserJoinCommand = new CustomCommandModel(command.UserJoinCommand) { IsEmbedded = true };
            this.CorrectAnswerCommand = new CustomCommandModel(command.CorrectAnswerCommand) { IsEmbedded = true };
            this.UserSuccessCommand = new CustomCommandModel(command.UserSuccessOutcome.Command) { IsEmbedded = true };
            this.UserFailureCommand = new CustomCommandModel(MixItUp.Base.Resources.GameSubCommand) { IsEmbedded = true };
        }
#pragma warning restore CS0612 // Type or member is obsolete

        private TriviaGameCommandModel() { }

        public override IEnumerable<CommandModelBase> GetInnerCommands()
        {
            List<CommandModelBase> commands = new List<CommandModelBase>();
            commands.Add(this.StartedCommand);
            commands.Add(this.UserJoinCommand);
            commands.Add(this.CorrectAnswerCommand);
            commands.Add(this.UserSuccessCommand);
            commands.Add(this.UserFailureCommand);
            return commands;
        }

        protected override async Task PerformInternal(CommandParametersModel parameters)
        {
            this.runParameters = parameters;

            bool useCustomQuestion = (RandomHelper.GenerateProbability() >= 50);
            if (this.CustomQuestions.Count > 0 && !this.UseRandomOnlineQuestions)
            {
                useCustomQuestion = true;
            }
            else if (this.CustomQuestions.Count == 0 && this.UseRandomOnlineQuestions)
            {
                useCustomQuestion = false;
            }

            if (!useCustomQuestion)
            {
                using (AdvancedHttpClient client = new AdvancedHttpClient())
                {
                    OpenTDBResults openTDBResults = await client.GetAsync<OpenTDBResults>(OpenTDBUrl);
                    if (openTDBResults != null && openTDBResults.results != null && openTDBResults.results.Count > 0)
                    {
                        List<string> answers = new List<string>();
                        answers.Add(HttpUtility.HtmlDecode(openTDBResults.results[0].correct_answer));
                        answers.AddRange(openTDBResults.results[0].incorrect_answers.Select(a => HttpUtility.HtmlDecode(a)));
                        this.question = new TriviaGameQuestionModel()
                        {
                            Question = HttpUtility.HtmlDecode(openTDBResults.results[0].question),
                            Answers = answers,
                        };
                    }
                }
            }

            if (this.CustomQuestions.Count > 0 && (useCustomQuestion || this.question == null))
            {
                this.question = this.CustomQuestions.Random();
            }

            if (this.question == null)
            {
                this.question = FallbackQuestion;
            }

            int i = 1;
            foreach (string answer in this.question.Answers.Shuffle())
            {
                this.numbersToAnswers[i] = answer;
                i++;
            }

            int correctAnswerNumber = 0;
            foreach (var kvp in this.numbersToAnswers)
            {
                if (kvp.Value.Equals(this.question.CorrectAnswer))
                {
                    correctAnswerNumber = kvp.Key;
                    break;
                }
            }

            this.runParameters.SpecialIdentifiers[TriviaGameCommandModel.GameTriviaQuestionSpecialIdentifier] = this.question.Question;
            this.runParameters.SpecialIdentifiers[TriviaGameCommandModel.GameTriviaAnswersSpecialIdentifier] = string.Join(", ", this.numbersToAnswers.OrderBy(a => a.Key).Select(a => $"{a.Key}) {a.Value}"));
            this.runParameters.SpecialIdentifiers[TriviaGameCommandModel.GameTriviaCorrectAnswerSpecialIdentifier] = this.question.CorrectAnswer;

            GlobalEvents.OnChatMessageReceived += GlobalEvents_OnChatMessageReceived;

            await this.StartedCommand.Perform(this.runParameters);

            await Task.Delay(this.TimeLimit * 1000);

            GlobalEvents.OnChatMessageReceived -= GlobalEvents_OnChatMessageReceived;

            List<CommandParametersModel> winners = new List<CommandParametersModel>();
            foreach (var kvp in this.runUserSelections.ToList())
            {
                CommandParametersModel participant = this.runUsers[kvp.Key];
                if (kvp.Value == correctAnswerNumber)
                {
                    winners.Add(participant);
                    this.PerformPrimarySetPayout(participant.User, this.WinAmount);
                    participant.SpecialIdentifiers[GameCommandModelBase.GamePayoutSpecialIdentifier] = this.WinAmount.ToString();
                    await this.UserSuccessCommand.Perform(participant);
                }
                else
                {
                    await this.UserFailureCommand.Perform(participant);
                }
            }

            this.runParameters.SpecialIdentifiers[GameCommandModelBase.GameWinnersSpecialIdentifier] = string.Join(", ", winners.Select(u => "@" + u.User.Username));
            this.runParameters.SpecialIdentifiers[GameCommandModelBase.GamePayoutSpecialIdentifier] = this.WinAmount.ToString();
            await this.CorrectAnswerCommand.Perform(this.runParameters);

            this.ClearData();
        }

        private async void GlobalEvents_OnChatMessageReceived(object sender, ViewModel.Chat.ChatMessageViewModel message)
        {
            try
            {
                if (!this.runUsers.ContainsKey(message.User) && !string.IsNullOrEmpty(message.PlainTextMessage) && int.TryParse(message.PlainTextMessage, out int choice) && this.numbersToAnswers.ContainsKey(choice))
                {
                    CommandParametersModel parameters = new CommandParametersModel(message.User, message.Platform, message.ToArguments());
                    this.runUsers[message.User] = parameters;
                    this.runUserSelections[message.User] = choice;
                    await this.UserJoinCommand.Perform(parameters);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void ClearData()
        {
            this.runParameters = null;
            this.question = null;
            this.numbersToAnswers.Clear();
            this.runUsers.Clear();
            this.runUserSelections.Clear();
        }
    }
}