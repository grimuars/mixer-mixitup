﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Util;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Actions
{
    public enum FileActionTypeEnum
    {
        SaveToFile,
        AppendToFile,
        ReadFromFile,
        ReadSpecificLineFromFile,
        ReadRandomLineFromFile,
        RemoveSpecificLineFromFile,
        RemoveRandomLineFromFile,
        InsertInFileAtSpecificLine,
        InsertInFileAtRandomLine,
    }

    [DataContract]
    public class FileActionModel : ActionModelBase
    {
        [DataMember]
        public FileActionTypeEnum ActionType { get; set; }

        [DataMember]
        public string FilePath { get; set; }

        [DataMember]
        public string TransferText { get; set; }

        [DataMember]
        public string LineIndex { get; set; }

        public FileActionModel(FileActionTypeEnum actionType, string filePath, string transferText, string lineIndex = null)
            : base(ActionTypeEnum.File)
        {
            this.ActionType = actionType;
            this.FilePath = filePath;
            this.TransferText = transferText;
            this.LineIndex = lineIndex;
        }

#pragma warning disable CS0612 // Type or member is obsolete
        internal FileActionModel(MixItUp.Base.Actions.FileAction action)
            : base(ActionTypeEnum.File)
        {
            this.ActionType = (FileActionTypeEnum)(int)action.FileActionType;
            this.FilePath = action.FilePath;
            this.TransferText = action.TransferText;
            this.LineIndex = action.LineIndexToRead;
        }
#pragma warning restore CS0612 // Type or member is obsolete

        private FileActionModel() { }

        protected override async Task PerformInternal(CommandParametersModel parameters)
        {
            string filePath = await ReplaceStringWithSpecialModifiers(this.FilePath, parameters);
            filePath = filePath.ToFilePathString();

            string textToWrite = string.Empty;
            string textToRead = string.Empty;
            List<string> lines = new List<string>();

            if (this.ActionType == FileActionTypeEnum.ReadFromFile ||
                this.ActionType == FileActionTypeEnum.ReadSpecificLineFromFile || this.ActionType == FileActionTypeEnum.ReadRandomLineFromFile ||
                this.ActionType == FileActionTypeEnum.RemoveSpecificLineFromFile || this.ActionType == FileActionTypeEnum.RemoveRandomLineFromFile)
            {
                parameters.SpecialIdentifiers.Remove(this.TransferText);
            }

            if (this.ActionType == FileActionTypeEnum.SaveToFile || this.ActionType == FileActionTypeEnum.AppendToFile ||
                this.ActionType == FileActionTypeEnum.InsertInFileAtSpecificLine || this.ActionType == FileActionTypeEnum.InsertInFileAtRandomLine)
            {
                textToWrite = await this.GetTextToSave(parameters);
            }

            if (this.ActionType == FileActionTypeEnum.ReadFromFile || this.ActionType == FileActionTypeEnum.AppendToFile ||
                this.ActionType == FileActionTypeEnum.ReadSpecificLineFromFile || this.ActionType == FileActionTypeEnum.ReadRandomLineFromFile ||
                this.ActionType == FileActionTypeEnum.RemoveSpecificLineFromFile || this.ActionType == FileActionTypeEnum.RemoveRandomLineFromFile ||
                this.ActionType == FileActionTypeEnum.InsertInFileAtSpecificLine || this.ActionType == FileActionTypeEnum.InsertInFileAtRandomLine)
            {
                textToRead = await ChannelSession.Services.FileService.ReadFile(filePath);
            }

            if (this.ActionType == FileActionTypeEnum.ReadSpecificLineFromFile || this.ActionType == FileActionTypeEnum.ReadRandomLineFromFile ||
                this.ActionType == FileActionTypeEnum.RemoveSpecificLineFromFile || this.ActionType == FileActionTypeEnum.RemoveRandomLineFromFile ||
                this.ActionType == FileActionTypeEnum.InsertInFileAtSpecificLine || this.ActionType == FileActionTypeEnum.InsertInFileAtRandomLine)
            {
                if (!string.IsNullOrEmpty(textToRead))
                {
                    lines = new List<string>(textToRead.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries));
                }

                int lineIndex = 0;
                if (this.ActionType == FileActionTypeEnum.ReadSpecificLineFromFile || this.ActionType == FileActionTypeEnum.RemoveSpecificLineFromFile ||
                    this.ActionType == FileActionTypeEnum.InsertInFileAtSpecificLine)
                {
                    string lineToRead = await ReplaceStringWithSpecialModifiers(this.LineIndex, parameters);
                    if (!int.TryParse(lineToRead, out lineIndex))
                    {
                        return;
                    }
                    lineIndex = lineIndex - 1;
                }
                else if (this.ActionType == FileActionTypeEnum.ReadRandomLineFromFile || this.ActionType == FileActionTypeEnum.RemoveRandomLineFromFile ||
                    this.ActionType == FileActionTypeEnum.InsertInFileAtRandomLine)
                {
                    lineIndex = RandomHelper.GenerateRandomNumber(lines.Count);
                }

                if (lineIndex >= 0)
                {
                    if (this.ActionType == FileActionTypeEnum.InsertInFileAtSpecificLine || this.ActionType == FileActionTypeEnum.InsertInFileAtRandomLine)
                    {
                        if (this.ActionType == FileActionTypeEnum.InsertInFileAtSpecificLine)
                        {
                            lineIndex = Math.Min(lines.Count, lineIndex);
                        }
                        lines.Insert(lineIndex, textToWrite);
                    }
                    else
                    {
                        if (lines.Count > lineIndex)
                        {
                            if (this.ActionType == FileActionTypeEnum.ReadSpecificLineFromFile || this.ActionType == FileActionTypeEnum.ReadRandomLineFromFile ||
                                this.ActionType == FileActionTypeEnum.RemoveRandomLineFromFile || this.ActionType == FileActionTypeEnum.RemoveSpecificLineFromFile)
                            {
                                textToRead = lines[lineIndex];
                            }

                            if (this.ActionType == FileActionTypeEnum.RemoveRandomLineFromFile || this.ActionType == FileActionTypeEnum.RemoveSpecificLineFromFile)
                            {
                                lines.RemoveAt(lineIndex);
                            }
                        }
                    }
                }
            }

            if (this.ActionType == FileActionTypeEnum.ReadFromFile ||
                this.ActionType == FileActionTypeEnum.ReadSpecificLineFromFile || this.ActionType == FileActionTypeEnum.ReadRandomLineFromFile ||
                this.ActionType == FileActionTypeEnum.RemoveSpecificLineFromFile || this.ActionType == FileActionTypeEnum.RemoveRandomLineFromFile)
            {
                if (!string.IsNullOrEmpty(textToRead))
                {
                    parameters.SpecialIdentifiers[this.TransferText] = textToRead;
                }
            }

            if (this.ActionType == FileActionTypeEnum.RemoveSpecificLineFromFile || this.ActionType == FileActionTypeEnum.RemoveRandomLineFromFile ||
                this.ActionType == FileActionTypeEnum.InsertInFileAtSpecificLine || this.ActionType == FileActionTypeEnum.InsertInFileAtRandomLine)
            {
                textToWrite = string.Join(Environment.NewLine, lines);
            }

            if (this.ActionType == FileActionTypeEnum.SaveToFile ||
                this.ActionType == FileActionTypeEnum.RemoveSpecificLineFromFile || this.ActionType == FileActionTypeEnum.RemoveRandomLineFromFile ||
                this.ActionType == FileActionTypeEnum.InsertInFileAtSpecificLine || this.ActionType == FileActionTypeEnum.InsertInFileAtRandomLine)
            {
                await ChannelSession.Services.FileService.SaveFile(filePath, textToWrite);
            }

            if (this.ActionType == FileActionTypeEnum.AppendToFile)
            {
                if (!string.IsNullOrEmpty(textToRead))
                {
                    textToWrite = Environment.NewLine + textToWrite;
                }
                await ChannelSession.Services.FileService.AppendFile(filePath, textToWrite);
            }
        }

        private async Task<string> GetTextToSave(CommandParametersModel parameters)
        {
            string textToWrite = (!string.IsNullOrEmpty(this.TransferText)) ? this.TransferText : string.Empty;
            return await ReplaceStringWithSpecialModifiers(textToWrite, parameters);
        }
    }
}
