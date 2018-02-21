//
// Copyright (C) 2018, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX.DirectWrite;
using SharpDX;
using SharpDX.Direct2D1;
using Point = System.Windows.Point;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class RangeCounter : Indicator
	{
		private bool	isRangeDerivate;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionRangeCounter;
				Name						= NinjaTrader.Custom.Resource.NinjaScriptIndicatorNameRangeCounter;
				Calculate					= Calculate.OnPriceChange;
				CountDown					= true;
				DisplayInDataBox			= false;
				DrawOnPricePanel			= false;
				IsOverlay					= true;
				IsChartOnly					= true;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsArray == null || BarsArray.Length == 0)
				return;

			if (BarsArray[0].BarsType.BuiltFrom == BarsPeriodType.Tick && BarsArray[0].BarsPeriod.ToString().IndexOf("Range") >= 0)
				isRangeDerivate = true;

			double high = High.GetValueAt(Bars.Count - 1 - (Calculate == NinjaTrader.NinjaScript.Calculate.OnBarClose ? 1 : 0));
			double low = Low.GetValueAt(Bars.Count - 1 - (Calculate == NinjaTrader.NinjaScript.Calculate.OnBarClose ? 1 : 0));
			double close = Close.GetValueAt(Bars.Count - 1 - (Calculate == NinjaTrader.NinjaScript.Calculate.OnBarClose ? 1 : 0));
			int actualRange = (int)Math.Round(Math.Max(close - low, high - close) / Bars.Instrument.MasterInstrument.TickSize);
			double rangeCount = CountDown ? BarsPeriod.Value - actualRange : actualRange;

			string range1 = (BarsPeriod.BarsPeriodType == BarsPeriodType.Range || isRangeDerivate ? CountDown
				? string.Format(NinjaTrader.Custom.Resource.RangeCounterRemaing, rangeCount) : string.Format(NinjaTrader.Custom.Resource.RangerCounterCount, rangeCount)
				: NinjaTrader.Custom.Resource.RangeCounterBarError);

			Draw.TextFixed(this, "NinjaScriptInfo", range1, TextPosition.BottomRight);
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "CountDown", Order = 1, GroupName = "NinjaScriptParameters")]
		public bool CountDown
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RangeCounter[] cacheRangeCounter;
		public RangeCounter RangeCounter(bool countDown)
		{
			return RangeCounter(Input, countDown);
		}

		public RangeCounter RangeCounter(ISeries<double> input, bool countDown)
		{
			if (cacheRangeCounter != null)
				for (int idx = 0; idx < cacheRangeCounter.Length; idx++)
					if (cacheRangeCounter[idx] != null && cacheRangeCounter[idx].CountDown == countDown && cacheRangeCounter[idx].EqualsInput(input))
						return cacheRangeCounter[idx];
			return CacheIndicator<RangeCounter>(new RangeCounter(){ CountDown = countDown }, input, ref cacheRangeCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RangeCounter RangeCounter(bool countDown)
		{
			return indicator.RangeCounter(Input, countDown);
		}

		public Indicators.RangeCounter RangeCounter(ISeries<double> input , bool countDown)
		{
			return indicator.RangeCounter(input, countDown);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RangeCounter RangeCounter(bool countDown)
		{
			return indicator.RangeCounter(Input, countDown);
		}

		public Indicators.RangeCounter RangeCounter(ISeries<double> input , bool countDown)
		{
			return indicator.RangeCounter(input, countDown);
		}
	}
}

#endregion
