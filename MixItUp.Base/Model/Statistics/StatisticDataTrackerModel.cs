﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Statistics
{
    public class StatisticDataPointModel
    {
        public string Identifier { get; private set; }

        public double ValueDecimal { get; private set; }
        public string ValueString { get; private set; }

        public DateTimeOffset DateTime { get; set; }

        public StatisticDataPointModel(string identifier) : this(identifier, -1) { }

        public StatisticDataPointModel(int value) : this(null, value) { }

        public StatisticDataPointModel(string identifier, int value) : this(identifier, (double)value) { }

        public StatisticDataPointModel(string identifier, string value)
            : this(identifier)
        {
            this.ValueString = value;
        }

        public StatisticDataPointModel(string identifier, double value)
        {
            this.Identifier = !string.IsNullOrEmpty(identifier) ? identifier : string.Empty;
            this.ValueDecimal = value;
            this.DateTime = DateTimeOffset.Now;
        }
    }

    public abstract class StatisticDataTrackerModelBase
    {
        public string Name { get; private set; }
        public string IconName { get; private set; }

        public DateTimeOffset StartTime { get; private set; }

        public List<StatisticDataPointModel> DataPoints { get; protected set; }

        private Func<StatisticDataTrackerModelBase, Task> updateFunction;

        public StatisticDataTrackerModelBase(string name, string iconName, Func<StatisticDataTrackerModelBase, Task> updateFunction)
        {
            this.Name = name;
            this.IconName = iconName;
            this.DataPoints = new List<StatisticDataPointModel>();
            this.updateFunction = updateFunction;

            this.StartTime = DateTimeOffset.Now;

            Task.Run(async () =>
            {
                while (true)
                {
                    await this.updateFunction(this);

                    await Task.Delay(60000);
                }
            });
        }

        public int TotalMinutes { get { return (int)Math.Max((DateTimeOffset.Now - this.StartTime).TotalMinutes, 1); } }

        public int UniqueIdentifiers { get { return this.DataPoints.Select(dp => dp.Identifier).Distinct().Count(); } }

        public double AverageUniqueIdentifiers { get { return ((double)this.UniqueIdentifiers) / ((double)this.TotalMinutes); } }

        public int Total { get { return this.DataPoints.Count; } }

        public double Average { get { return ((double)this.Total) / ((double)this.TotalMinutes); } }
        public string AverageString { get { return Math.Round(this.Average, 2).ToString(); } }

        public int MaxValue { get { return (int)this.MaxValueDecimal; } }
        public string MaxValueString { get { return this.MaxValue.ToString(); } }
        public double MaxValueDecimal { get { return (this.DataPoints.Count > 0) ? this.DataPoints.Select(dp => dp.ValueDecimal).ToArray().Max() : 0.0; } }
        public string MaxValueDecimalString { get { return Math.Round(this.MaxValueDecimal, 2).ToString(); } }

        public int TotalValue { get { return (int)this.TotalValueDecimal; } }
        public double TotalValueDecimal { get { return this.DataPoints.Select(dp => dp.ValueDecimal).ToArray().Sum(); } }

        public double AverageValue { get { return this.TotalValueDecimal / ((double)this.TotalMinutes); } }
        public string AverageValueString { get { return Math.Round(AverageValue, 2).ToString(); } }

        public double LastValue { get { return (this.DataPoints.Count > 0) ? this.DataPoints.Last().ValueDecimal : 0.0; } }
        public string LastValueString { get { return Math.Round(LastValue, 2).ToString(); } }

        public abstract IEnumerable<string> GetExportHeaders();

        public virtual IEnumerable<List<string>> GetExportData()
        {
            List<List<string>> results = new List<List<string>>();
            foreach (StatisticDataPointModel dataPoint in this.DataPoints)
            {
                List<string> resultRow = new List<string>();

                if (!string.IsNullOrEmpty(dataPoint.Identifier))
                {
                    resultRow.Add(dataPoint.Identifier);
                }

                if (dataPoint.ValueDecimal >= 0)
                {
                    resultRow.Add(dataPoint.ValueDecimal.ToString());
                }

                if (!string.IsNullOrEmpty(dataPoint.ValueString))
                {
                    resultRow.Add(dataPoint.ValueString);
                }

                resultRow.Add(dataPoint.DateTime.LocalDateTime.ToString("MM/dd/yy HH:mm"));

                results.Add(resultRow);
            }
            return results;
        }
    }

    public class StaticTextStatisticDataTrackerModel : StatisticDataTrackerModelBase
    {
        public StaticTextStatisticDataTrackerModel(string name, string iconName, Func<StatisticDataTrackerModelBase, Task> updateFunction)
            : base(name, iconName, updateFunction)
        { }

        public void AddValue(string identifier, string value) { this.DataPoints.Add(new StatisticDataPointModel(identifier, value)); }

        public void ClearValues() { this.DataPoints.Clear(); }

        public override IEnumerable<string> GetExportHeaders() { return this.DataPoints.Select(dp => dp.Identifier); }

        public override IEnumerable<List<string>> GetExportData()
        {
            List<List<string>> results = new List<List<string>>();
            results.Add(new List<string>());
            foreach (StatisticDataPointModel dataPoint in this.DataPoints)
            {
                results[0].Add(dataPoint.ValueString);
            }
            return results;
        }

        public override string ToString()
        {
            List<string> values = new List<string>();
            foreach (StatisticDataPointModel dataPoint in this.DataPoints)
            {
                values.Add(string.Format("{0}: {1}", dataPoint.Identifier, dataPoint.ValueString));
            }
            return string.Join(",    ", values);
        }
    }

    public class TrackedNumberStatisticDataTrackerModel : StatisticDataTrackerModelBase
    {
        public TrackedNumberStatisticDataTrackerModel(string name, string iconName, Func<StatisticDataTrackerModelBase, Task> updateFunction)
            : base(name, iconName, updateFunction)
        { }

        public void AddValue(int value) { this.DataPoints.Add(new StatisticDataPointModel(value)); }

        public override IEnumerable<string> GetExportHeaders()
        {
            return new List<string>()
            {
                Resources.Current,
                Resources.Max,
                Resources.Average,
            };
        }

        public override IEnumerable<List<string>> GetExportData()
        {
            List<List<string>> results = new List<List<string>>();
            results.Add(new List<string>() { this.LastValueString, this.MaxValueString, this.AverageValueString });
            return results;
        }

        public override string ToString()
        {
            return $"{Resources.Current}: {this.LastValueString},    {Resources.Max}: {this.MaxValueString},    {Resources.Average}: {this.AverageValueString}";
        }
    }

    public class EventStatisticDataTrackerModel : StatisticDataTrackerModelBase
    {
        private event EventHandler<string> StatisticEventOccurred;
        private event EventHandler<Tuple<string, double>> StatisticEventWithDecimalValueOccurred;
        private event EventHandler<Tuple<string, string>> StatisticEventWithStringValueOccurred;

        private IEnumerable<string> exportHeaders;

        private Func<EventStatisticDataTrackerModel, string> customToStringFunction;

        public EventStatisticDataTrackerModel(string name, string iconName, IEnumerable<string> exportHeaders, Func<EventStatisticDataTrackerModel, string> customToStringFunction = null)
            : base(name, iconName, (StatisticDataTrackerModelBase stats) => { return Task.CompletedTask; })
        {
            this.exportHeaders = exportHeaders;
            this.customToStringFunction = customToStringFunction;

            this.StatisticEventOccurred += EventStatisticsDataTracker_StatisticEventOccurred;
            this.StatisticEventWithDecimalValueOccurred += EventStatisticsDataTracker_StatisticEventWithDecimalValueOccurred;
            this.StatisticEventWithStringValueOccurred += EventStatisticDataTracker_StatisticEventWithStringValueOccurred;
        }

        public override IEnumerable<string> GetExportHeaders() { return this.exportHeaders; }

        public void AddValue(string identifier) { this.DataPoints.Add(new StatisticDataPointModel(identifier)); }

        public void AddValue(string identifier, double value) { this.DataPoints.Add(new StatisticDataPointModel(identifier, value)); }

        public void AddValue(string identifier, string value) { this.DataPoints.Add(new StatisticDataPointModel(identifier, value)); }

        public void OnStatisticEventOccurred(string key)
        {
            if (this.StatisticEventOccurred != null)
            {
                this.StatisticEventOccurred(this, key);
            }
        }

        public void OnStatisticEventOccurred(string key, double value)
        {
            if (this.StatisticEventWithDecimalValueOccurred != null)
            {
                this.StatisticEventWithDecimalValueOccurred(this, new Tuple<string, double>(key, value));
            }
        }

        public void OnStatisticEventOccurred(string key, string value)
        {
            if (this.StatisticEventWithStringValueOccurred != null)
            {
                this.StatisticEventWithStringValueOccurred(this, new Tuple<string, string>(key, value));
            }
        }

        public override string ToString()
        {
            if (this.customToStringFunction != null)
            {
                return this.customToStringFunction(this);
            }
            return $"{Resources.Total}: {this.UniqueIdentifiers},    {Resources.Average}: {this.AverageUniqueIdentifiers}";
        }

        private void EventStatisticsDataTracker_StatisticEventOccurred(object sender, string e)
        {
            this.AddValue(e);
        }

        private void EventStatisticsDataTracker_StatisticEventWithDecimalValueOccurred(object sender, Tuple<string, double> e)
        {
            this.AddValue(e.Item1, e.Item2);
        }

        private void EventStatisticDataTracker_StatisticEventWithStringValueOccurred(object sender, Tuple<string, string> e)
        {
            this.AddValue(e.Item1, e.Item2);
        }
    }
}
