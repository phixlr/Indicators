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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using System.IO;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Adaptation of Morretech Swing system intended for 15min bars and longer
	/// Developed by Warren Hansen at TradeStrat on 7/20/2017
	/// whansen1@mac.com
	/// </summary>
	
	public class MooreTechSwing02 : Indicator
	{
	
		private struct SwingData
		{
			public  double 	lastHigh 		{ get; set; }
			public	int 	lastHighBarnum	{ get; set; }
			public	double 	lastLow 		{ get; set; }
			public	int 	lastLowBarnum	{ get; set; }
			public  double 	prevHigh		{ get; set; }
			public	int 	prevHighBarnum	{ get; set; }
			public	double 	prevLow			{ get; set; }
			public	int 	prevLowBarnum	{ get; set; }
		}
		
		private struct EntryData
		{		
			public	double 	longEntryPrice 	{ get; set; }
			public	int 	longEntryBarnum	{ get; set; }
			public	double 	shortEntryPrice { get; set; }
			public	int 	shortEntryBarnum { get; set; }
			public	int 	shortLineLength { get; set; }
			public	int 	longLineLength 	{ get; set; }
			public 	bool	inLongTrade 	{ get; set; }
			public 	bool	inShortTrade 	{ get; set; }
			/// actual entry
			public	double 	shortEntryActual { get; set; }
			public	double 	longEntryActual { get; set; }
			public	int 	barsSinceEntry 	{ get; set; }
			/// hard stops
			public	int 	longHardStopBarnum	{ get; set; }
			public	int 	shortHardStopBarnum	{ get; set; }
			public	double 	hardStopLine 	{ get; set; }
		
			/// pivot stops
			public	int 	longPivStopBarnum	{ get; set; }
			public	int 	shortPivStopBarnum	{ get; set; }
			public	int 	pivStopCounter	{ get; set; }
			public	double 	lastPivotValue 	{ get; set; }
			public	int 	pivLineLength	{ get; set; }  
		}
		
		private struct TradeData
		{
			public 	string	signalName 		{ get; set; }
			public 	double	lastShort 		{ get; set; }
			public 	double	lastLong 		{ get; set; }
			public	double 	tradeNum 		{ get; set; }
			public	double 	numWins 		{ get; set; }
			public 	double	tradeProfit		{ get; set; }
			public 	double	totalProfit		{ get; set; }
			public 	double	winTotal		{ get; set; }
			public 	double	lossTotal		{ get; set; }
			public 	double	profitFactor	{ get; set; }
			public 	double	pctWin			{ get; set; }
			public 	double	largestLoss		{ get; set; }
			public 	double	cost			{ get; set; }
			public 	double	roi				{ get; set; }
			public 	double	openProfit		{ get; set; }
			public 	double	largestOpenDraw	{ get; set; }
			public 	string	report 			{ get; set; }
			public 	string	reportSimple	{ get; set; }
			public 	string	csvFile			{ get; set; }
		}
		
		private Swing 		Swing1;	
		private FastPivotFinder FastPivotFinder1;
		
		private Brush 	upColor; 
		private Brush 	downColor;
		private Brush 	textColor;
		
		private Series<int> signals;
		private SwingData swingData = new SwingData{};
		private EntryData entry = new EntryData{};
		private TradeData tradeData = new TradeData{};
		// alter for confirmation count
		private bool secondPivStopFlag = false;
		int cofirmationCount = 1;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "_MooreTech Swing 02";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
				/// inputs 
				shares				= 100;				/// 	int		shares			= 100;
				swingPct			= 0.005;			///		double  swingPct		= 0.005;
				minBarsToLastSwing 	= 70;				/// 	int MinBarsToLastSwing 	= 70;
				enableHardStop 		= true;				/// 	bool setHardStop = true, int pctHardStop 3, 
				pctHardStop  		= 3;
				enablePivotStop 	= true;				/// 	bool setPivotStop = true, int pivotStopSwingSize = 5, 
				pivotStopSwingSize 	= 5;
				pivotStopPivotRange = 0.2;				///		double pivotStopPivotSlop = 0.2
				/// swow plots
				showUpCount 			= false;		/// 	bool ShowUpCount 			= false;
				showHardStops 			= false;		/// 	bool show hard stops 		= false;
				printTradesOnChart		= false;		/// 	bool printtradesOn Chart	= false
				printTradesSimple 		= false;		/// 	bool printTradesSimple 		= false
				printTradesTolog 		= true;			/// 	bool printTradesTolog 		= true;
			}
			else if (State == State.Configure)
			{
				upColor 	= Brushes.LimeGreen;
				downColor	= Brushes.Crimson;
				textColor	= Brushes.Crimson;
            }
			else if(State == State.DataLoaded)
			  {
				  ClearOutputWindow();     
				  Swing1				= Swing(5);	// for piv stops
				  FastPivotFinder1 = FastPivotFinder(false, false, 70, 0.005, 1);
				  signals = new Series<int>(this, MaximumBarsLookBack.Infinite); // for starategy integration
			  } 
		}
		
		///******************************************************************************************************************************
		/// 
		/// 										on bar update
		/// 
		/// ****************************************************************************************************************************
		protected override void OnBarUpdate()
		{
			if ( CurrentBar < 20 ) { resetStruct(doIt: true); return; }
			
			resetBarsSinceEntry();
			findSwings();

			findShortEntry();
			findLongeEntry();
			
			drawLongEntryLine(inLongTrade: entry.inLongTrade);
			drawShortEntryLine(inShortTrade: entry.inShortTrade);
			
			showEntryDelay();
			
			if( enableHardStop ) { setHardStop(pct: pctHardStop, shares: shares, plot: showHardStops);}
			if ( enablePivotStop ) { setPivotStop(swingSize: pivotStopSwingSize, pivotSlop: pivotStopPivotRange); }
			
			recordTrades(printChart: true, printLog: true, hiLow: true,  simple: true);
			///	these functions under developement and disabled
			///  MUCH BETTER! 41%, 48%,  8%, 37%, -.01% 5yr performance
			///  upload to VPS
			///  Un-Solved Optimizer - Optimization Fails with bar21, index out of range
			///  Solve playback + observe trades in action solve USO

			///  problem solve USO , show entry names, print log for each bar
			/// 1. entry line, Hi Low
			///  clean up stream writer + upload to VPS
			///  clac position size from 1. account size, 2. number of strategies
			///  make FOMC look for neg afffects
			///  should Massive Gap Cause no Entry?
			///  Find another ETF to invest in like GUSH or USO
		}
		
		public void checkForNil(object myobj) {
			if (myobj == null) {
				Print( CurrentBar.ToString() +  " Nil: " + myobj.ToString() );
			}
		}
		
		
		public void findSwings() {
			/// attempt to tighten swing confirmation after 2nd piv stop out
			if ( secondPivStopFlag ) {
				cofirmationCount = 3;
				BarBrush = Brushes.Goldenrod;
				CandleOutlineBrush = Brushes.Goldenrod;
			
				swingData.lastHigh 			= FastPivotFinder(false, false, 70, 0.005, cofirmationCount).LastHigh[0];
				swingData.lastHighBarnum	= FastPivotFinder(false, false, 70, 0.005, cofirmationCount).LastHighBarnum;
				swingData.lastLow 			= FastPivotFinder(false, false, 70, 0.005, cofirmationCount).LastLow[0];
				swingData.lastLowBarnum		= FastPivotFinder(false, false, 70, 0.005, cofirmationCount).LastLowBarnum;
				swingData.prevHigh			= FastPivotFinder(false, false, 70, 0.005, cofirmationCount).PrevHigh;
				swingData.prevHighBarnum	= FastPivotFinder(false, false, 70, 0.005, cofirmationCount).PrevHighBarnum;
				swingData.prevLow			= FastPivotFinder(false, false, 70, 0.005, cofirmationCount).PrevLow;
				swingData.prevLowBarnum		= FastPivotFinder(false, false, 70, 0.005, cofirmationCount).PrevLowBarnum;	
				
			} else {
				cofirmationCount = 1;
			
				swingData.lastHigh 			= FastPivotFinder1.LastHigh[0];
				swingData.lastHighBarnum	= FastPivotFinder1.LastHighBarnum;
				swingData.lastLow 			= FastPivotFinder1.LastLow[0];
				swingData.lastLowBarnum		= FastPivotFinder1.LastLowBarnum;
				swingData.prevHigh			= FastPivotFinder1.PrevHigh;
				swingData.prevHighBarnum	= FastPivotFinder1.PrevHighBarnum;
				swingData.prevLow			= FastPivotFinder1.PrevLow;
				swingData.prevLowBarnum		= FastPivotFinder1.PrevLowBarnum;	
			}
		}

		public void resetBarsSinceEntry() {
			if ( entry.inShortTrade == true) { entry.barsSinceEntry = CurrentBar - entry.shortEntryBarnum; }
			else if ( entry.inLongTrade == true) { entry.barsSinceEntry = CurrentBar - entry.longEntryBarnum; }
			else { entry.barsSinceEntry = 0;}
		}
		
		
		///******************************************************************************************************************************
		/// 
		/// 										set Pivot Stop
		/// 
		/// ****************************************************************************************************************************
		public void setPivotStop(int swingSize, double pivotSlop) {
			
			double lastSwingLow = Swing1.SwingLow[ swingSize ];
			double lastSwingHigh = Swing1.SwingHigh[ swingSize ];
			
			/// long pivots, count pivots above entry for 2nd piv stop if  short /// close > entryswing 
			 if ( entry.inLongTrade && (( lastSwingLow + pivotSlop ) <  entry.lastPivotValue ) && entry.barsSinceEntry > 8 ) {
				 entry.pivStopCounter++;
				 Draw.Text(this, "LowSwingtxt"+ CurrentBar.ToString(),  entry.pivStopCounter.ToString(), swingSize, Low[swingSize] - (TickSize * 10));
				 entry.lastPivotValue = lastSwingLow;
			 }
			 /// short pivots, count pivots above entry for 2nd piv stop if  short /// close > entryswing 
			 if ( entry.inShortTrade && ( lastSwingHigh - pivotSlop )  > entry.lastPivotValue && entry.barsSinceEntry > 8 ) {
				 entry.pivStopCounter++;
				 Text myText = Draw.Text(this, "HighSwingtxt"+ CurrentBar.ToString(), entry.pivStopCounter.ToString(), swingSize, High[swingSize] + (TickSize * 10));
				 entry.lastPivotValue = lastSwingHigh;
			 }
			 /// draw the 2nd piv stop line //drawPivStops();
			 if(entry.inLongTrade || entry.inShortTrade )
			 if ( entry.pivStopCounter == 2) {
				int lineLength = 0; 
				entry.pivLineLength++; 
				RemoveDrawObject("pivStop" + (CurrentBar - 1));
				Draw.Line(this, "pivStop"  +CurrentBar.ToString(), false, entry.pivLineLength, entry.lastPivotValue, 0, 
						entry.lastPivotValue, Brushes.Magenta, DashStyleHelper.Dot, 2);
			 } 
			/// exit at pivot line
			exitFromPivotStop(pivotSlop: pivotSlop);
		}

		/// exit trade after pivot stop
		public void exitFromPivotStop(double pivotSlop) {
			
			if (CurrentBar > entry.longEntryBarnum &&  entry.pivStopCounter >= 2 && entry.inLongTrade && Low[0] <= entry.lastPivotValue ) {
				Draw.Dot(this, "testDot"+CurrentBar, true, 0, entry.lastPivotValue, Brushes.Magenta);
				entry.inLongTrade = false;
                signals[0]  = 2;
				entry.longPivStopBarnum = CurrentBar;
				tradeData.signalName = "LX - PS";
				entry.pivLineLength = 0;
				entry.pivStopCounter = 0;
				entry.barsSinceEntry = 0;
				secondPivStopFlag = true;
			}
			
			if (CurrentBar > entry.shortEntryBarnum &&  entry.pivStopCounter >= 2 && entry.inShortTrade && High[0] >= entry.lastPivotValue ) {
				Draw.Dot(this, "testDot"+CurrentBar, true, 0, entry.lastPivotValue, Brushes.Magenta);
				entry.inShortTrade = false;
                signals[0] = -2;	
				entry.shortPivStopBarnum = CurrentBar;
				tradeData.signalName = "SX - PS";
				entry.pivLineLength = 0;
				entry.pivStopCounter = 0;
				entry.barsSinceEntry = 0;
				secondPivStopFlag = true;
			}
		}
		///******************************************************************************************************************************
		/// 
		/// 										set Hard Stop
		/// 
		/// ****************************************************************************************************************************
		public void setHardStop(double pct, int shares, bool plot) {
			/// find long entry price /// calc trade cost
			if (CurrentBar == entry.longEntryBarnum ) {
				double pctPrice = pct * 0.01;
				entry.hardStopLine = Math.Abs(Close[0]  - ( Close[0] * pctPrice));
				}
			/// find short entry price /// calc trade cost
			if (CurrentBar == entry.shortEntryBarnum ) {
				double pctPrice = pct * 0.01;
				entry.hardStopLine = Math.Abs(Close[0]  + ( Close[0] * pctPrice));
			}
			/// draw hard stop line
			drawHardStops(plot: plot);
			/// exit at hard stop
			exitFromStop();
		}
		
		/// exit at hard stop
		public void exitFromStop() {
			if ( entry.inLongTrade && Low[0] <= entry.hardStopLine ) {
				/// need short trades to debug this no long stops hit
				entry.inLongTrade = false;
                signals[0] = 2;
				entry.longHardStopBarnum	= CurrentBar;
				tradeData.signalName = "LX - HS";
				entry.barsSinceEntry = 0;
			} else if ( entry.inShortTrade && High[0] >= entry.hardStopLine ) {
				/// need short trades to debug this no long stops hit
				entry.inShortTrade = false;
                signals[0] = -2;
				entry.shortHardStopBarnum	= CurrentBar;
				tradeData.signalName = "SX - HS";
				entry.barsSinceEntry = 0;
			}
		}
		
		public void drawHardStops(bool plot) {
			if( !plot ) {
				return;
			}
			/// draw hard stop line 
			int lineLength = 0;
			string lineName = "";
			if ( entry.inLongTrade ) { 
				lineLength = entry.barsSinceEntry; 
				lineName = "hardStopLong";
			}
			if ( entry.inShortTrade ) { 
				lineLength = CurrentBar - entry.shortEntryBarnum; 
				lineName = "hardStopShort";
			}
			if(entry.barsSinceEntry > 1)
				RemoveDrawObject(lineName + (CurrentBar - 1));
			Draw.Line(this, lineName +CurrentBar.ToString(), false, lineLength, entry.hardStopLine, 0, 
					entry.hardStopLine, Brushes.DarkGray, DashStyleHelper.Dot, 2);
		}
		///******************************************************************************************************************************
		/// 
		/// 										RECORD TRADES
		/// 
		/// ****************************************************************************************************************************
		public void recordTrades(bool printChart, bool printLog, bool hiLow, bool simple){
			
		    /// calc short profit at long entry
			if (CurrentBar == entry.longEntryBarnum ) {
				tradeData.tradeNum++;
				if(checkForFirstEntry()) { 
					tradeData.tradeProfit = 0;
					tradeData.lastLong = entry.longEntryPrice;
					calcAndShowOnChart(printChart: printChart, printLog: printLog, hiLow: hiLow, simple: simple);
					return; }
				tradeData.tradeProfit =  entry.shortEntryActual - entry.longEntryActual;
			 	tradeData.lastLong = entry.longEntryPrice;
				calcAndShowOnChart(printChart: printChart, printLog: printLog, hiLow: hiLow, simple: simple);
			} else
			/// calc long profit at short entry
			if (CurrentBar == entry.shortEntryBarnum ) {
				tradeData.tradeNum++;
				if(checkForFirstEntry()) { 
					tradeData.tradeProfit =  0;
			 		tradeData.lastShort = entry.shortEntryPrice;
					calcAndShowOnChart(printChart: printChart, printLog: printLog, hiLow: hiLow, simple: simple);
					return; }
			  	tradeData.tradeProfit =  entry.shortEntryActual - entry.longEntryActual; //entry.shortEntryPrice - tradeData.lastLong;
			 	tradeData.lastShort = entry.shortEntryPrice;
				calcAndShowOnChart(printChart: printChart, printLog: printLog, hiLow: hiLow, simple: simple);
			 } else
			/// calc loss from short hard stop hit
			if ( CurrentBar == entry.shortHardStopBarnum  ) {
				tradeData.tradeNum++;
			    tradeData.tradeProfit = entry.shortEntryPrice -  entry.hardStopLine;
			 	tradeData.lastShort = entry.shortEntryPrice;
				calcAndShowOnChart(printChart: printChart, printLog: printLog, hiLow: hiLow, simple: simple);
			} else
			/// calc loss from long hard stop hit
			if ( CurrentBar == entry.longHardStopBarnum || CurrentBar == entry.longPivStopBarnum  ) {	
				tradeData.tradeNum++;
			  	tradeData.tradeProfit = entry.hardStopLine - entry.longEntryPrice;
			 	tradeData.lastLong = entry.longEntryPrice;
				calcAndShowOnChart(printChart: printChart, printLog: printLog, hiLow: hiLow, simple: simple);
			} else
			/// calc loss from short piv stop hit
			if (  CurrentBar == entry.shortPivStopBarnum ) {
				Draw.Dot(this, "sps"+CurrentBar, true, 0, Close[0], Brushes.Magenta);
				tradeData.tradeNum++;
			  	tradeData.tradeProfit = entry.shortEntryActual -  Close[0];
			 	tradeData.lastShort = entry.shortEntryPrice;
				calcAndShowOnChart(printChart: printChart, printLog: printLog, hiLow: hiLow, simple: simple);
			} 
			/// calc loss from long piv stop hit
			if (  CurrentBar == entry.longPivStopBarnum ) {
				Draw.Dot(this, "lps"+CurrentBar, true, 0, Close[0], Brushes.Magenta);
				tradeData.tradeNum++;
			  	tradeData.tradeProfit = entry.longEntryActual -  Close[0];
			 	tradeData.lastLong = entry.longEntryPrice;
				calcAndShowOnChart(printChart: printChart, printLog: printLog, hiLow: hiLow, simple: simple);
			} 
		}
		
		public bool checkForFirstEntry() {
			if ( CurrentBar == entry.longEntryBarnum && tradeData.tradeNum == 1) { 
				tradeData.lastLong = entry.longEntryPrice;
				return true;
			} else if (CurrentBar == entry.shortEntryBarnum && tradeData.tradeNum == 1) {	  
				tradeData.lastShort = entry.shortEntryPrice;
				return true;
			} else { return false;}	
		}
		
/// report results
		public void calcAndShowOnChart(bool printChart, bool printLog, bool hiLow, bool simple) {
			calcTradeStats();
			concatStats();
			if( printChart ) { customDrawTrades(show: hiLow, simple: simple);}
			
			if ( printLog ) {
				Print("\n"+Time[0].ToString() +" "+ tradeData.report );
				debugEntry(isOn:true); }
		}
		
		public void debugEntry(bool isOn){
			string debugText = "LE Price "+ entry.longEntryPrice +"  LastHigh "+ swingData.lastHigh  +"  Last Low "+ swingData.lastLow ;
			Print(debugText);
			
		}
		
		public void calcTradeStats() {
			tradeData.totalProfit = tradeData.totalProfit + tradeData.tradeProfit;
			if (tradeData.tradeProfit >= 0 ) {
				tradeData.numWins++; 
				tradeData.winTotal = tradeData.winTotal + tradeData.tradeProfit;
			}
			if (tradeData.tradeProfit < 0 ) {
				tradeData.lossTotal = tradeData.lossTotal + tradeData.tradeProfit; 
				if ( tradeData.tradeProfit < tradeData.largestLoss) {
					tradeData.largestLoss = tradeData.tradeProfit;
				}
			}
			tradeData.pctWin = (tradeData.numWins / tradeData.tradeNum) * 100;
			tradeData.profitFactor = (tradeData.winTotal / tradeData.lossTotal) * -1;
			tradeData.cost = (SMA(100)[0] * (double)shares ) / 3;
			tradeData.roi = ( ( tradeData.totalProfit * (double)shares ) / tradeData.cost ) * 100;
		}
		
		///  full report and simple
		public void concatStats(){
				string allStats = "#" + tradeData.tradeNum.ToString() + " " + tradeData.signalName + "  $" + tradeData.tradeProfit.ToString("0.00");
				//allStats = allStats + "\n" + tradeData.totalProfit.ToString("0.00") + " pts" + " " + tradeData.pctWin.ToString("0.0") + "%";
				tradeData.reportSimple = allStats;
			
				allStats = "#" + tradeData.tradeNum.ToString() + " " + tradeData.signalName + "  $" + tradeData.tradeProfit.ToString("0.00");
				allStats = allStats + "\n" + tradeData.totalProfit.ToString("0.00") + " pts" + " " + tradeData.pctWin.ToString("0.0") + "%";
				allStats = allStats + "\n" + tradeData.profitFactor.ToString("0.00") + "Pf  LL " + tradeData.largestLoss.ToString("0.00");
				allStats = allStats + "\n$" + tradeData.cost.ToString("0") + " Cost " + tradeData.roi.ToString("0.0") + "% Roi"; 
				writeToCsv();
				//tradeData.signalName = "*";
				tradeData.report = allStats;
		}
		
		public void writeToCsv() {
			/*
			// example of object instantiated which need to be disposed
			StreamWriter writer = new StreamWriter("some_file.txt");
			 
			// use the object
			writer.WriteLine("Some text");
			 
			// implements IDisposbile, make sure to call .Dispose() when finished
			writer.Dispose();
			*/
			string thisDate = "_"+DateTime.Now.ToString("yyyy-M-dd");
			
			var	filePath = @"C:\Users\MBPtrader\Documents\NT_CSV\" + Instrument.MasterInstrument.Name + thisDate+".csv" ;
			//before your loop
		    var csv = new StringBuilder();

			string titleRow = "Trade"+","+"Date"+","+"Signal"+","+"Profit"+","+"Sum Profit"+","+"Pct Win"+","+"PF"+","+"LL"+","+"Cost"+","+"ROI";
			string allStats = tradeData.tradeNum.ToString() + "," +Time[0].ToString("M-dd-yyyy")+","+ tradeData.signalName + "," + tradeData.tradeProfit.ToString("0.00");
			allStats = allStats + "," + tradeData.totalProfit.ToString("0.00") + "," +  tradeData.pctWin.ToString("0.0");
			allStats = allStats + "," + tradeData.profitFactor.ToString("0.00") + "," + tradeData.largestLoss.ToString("0.00");
			allStats = allStats + "," + tradeData.cost.ToString("0") + "," + tradeData.roi.ToString("0.0"); 

			// create the file 
			if ( tradeData.tradeNum == 1 ) {
		    	csv.AppendLine(titleRow);
				csv.AppendLine(allStats); 
				File.WriteAllText(filePath, csv.ToString());
			} else if (allStats != tradeData.csvFile ) {
		    	csv.AppendLine(allStats);  
				File.AppendAllText(filePath, csv.ToString());
			}
			tradeData.csvFile = allStats;
		}
		
		public void customDrawTrades(bool show, bool simple) {
			// set color
			if( tradeData.tradeProfit >= 0 ) { textColor = upColor;
			} else { textColor = downColor;}
			// set text
			string reportData = tradeData.report;
			if(simple) { reportData = tradeData.reportSimple;}
			if(show) {
			if (CurrentBar == entry.longEntryBarnum ) {
				Draw.Text(this, "LE"+CurrentBar, reportData, 0, MIN(Low, 20)[0] - (TickSize * 5), textColor); }
			if (CurrentBar == entry.shortEntryBarnum ) {
				Draw.Text(this, "SE"+CurrentBar, reportData, 0, MAX(High, 20)[0] + (TickSize * 5), textColor); }
			if (CurrentBar == entry.shortHardStopBarnum ) {
				Draw.Text(this, "SXh"+CurrentBar, reportData, 0, MAX(High, 20)[0] + (TickSize * 5), textColor); }
			if (CurrentBar == entry.longHardStopBarnum ) {
				Draw.Text(this, "LXh"+CurrentBar, reportData, 0, MIN(Low, 20)[0] - (TickSize * 5), textColor); }
			if (CurrentBar == entry.shortPivStopBarnum ) {
				Draw.Text(this, "SXp"+CurrentBar, reportData, 0, MAX(High, 20)[0] + (TickSize * 5), textColor); }
			if (CurrentBar == entry.longPivStopBarnum ) {
				Draw.Text(this, "LXp"+CurrentBar, reportData, 0, MIN(Low, 20)[0] - (TickSize * 5), textColor); }
			} else {
				Draw.Text(this, "report"+CurrentBar, reportData, 0, MIN(Low, 20)[0]);
			}
		}
		
		/// Long Entry Line ************************************************************************************************************
		public void drawLongEntryLine(bool inLongTrade){
			if ( inLongTrade ) { return; }
			entry.longLineLength++ ;
			if ( entry.longEntryPrice != 0 ) {
				if(DrawObjects["LeLine"+(CurrentBar-1).ToString()] != null)
					RemoveDrawObject("LeLine"+ (CurrentBar - 1));
				Draw.Line(this, "LeLine"+CurrentBar.ToString(), false, entry.longLineLength, entry.longEntryPrice, 0, 
						entry.longEntryPrice, Brushes.LimeGreen, DashStyleHelper.Solid, 4);
				showLongEntryArrow(inLongTrade: entry.inLongTrade);
			}	
		}
		/// <summary>
		///  Long Entry Arrow 
		/// </summary>
		/// <param name="inLongTrade"></param>
		public void showLongEntryArrow(bool inLongTrade){	
			if ( inLongTrade ) { return; }	
			if(entry.longEntryPrice == null) { return; }
			if ( High[0] > entry.longEntryPrice && Low[0] < entry.longEntryPrice ) {
				//Draw.Text(this, "LE"+CurrentBar.ToString(), "LE", 0, entry.longEntryPrice - (TickSize * 10), Brushes.LimeGreen);
				//customDrawTrades( show: true,  simple: false);
				ArrowUp myArrowUp = Draw.ArrowUp(this, "LEmade"+ CurrentBar.ToString(), true, 0, entry.longEntryPrice - (TickSize * 5), Brushes.LimeGreen);
				signals[0] = 1;
				secondPivStopFlag = false;
				//debugEntry(isOn:true);
			}
		}

		/// Short Entry Line ************************************************************************************************************
		public void drawShortEntryLine(bool inShortTrade){
			if ( inShortTrade ) {return;}
			entry.shortLineLength++ ;
			if ( entry.shortEntryPrice != 0 ) {
				if(DrawObjects["SeLine"+(CurrentBar-1).ToString()] != null)
					RemoveDrawObject("SeLine"+ (CurrentBar - 1));
				Draw.Line(this, "SeLine"+CurrentBar.ToString(), false, entry.shortLineLength, entry.shortEntryPrice, 0, 
					entry.shortEntryPrice, Brushes.Red, DashStyleHelper.Solid, 4);
				showShortEntryArrow(inShortTrade: entry.inShortTrade);
			}	
		}
		
		/// <summary>
		///  Short Entry Arrow 
		/// </summary>
		/// <param name="inLongTrade"></param>		
		public void showShortEntryArrow(bool inShortTrade) {
			if ( inShortTrade ) {return;}
			if(entry.shortEntryPrice == null) { return; }
			if ( High[0] > entry.shortEntryPrice && Low[0] < entry.shortEntryPrice ) {
				//Draw.Text(this, "SE"+CurrentBar.ToString(), "SE", 0, entry.shortEntryPrice + (TickSize * 10), Brushes.Crimson);
				ArrowDown myArrowDn = Draw.ArrowDown(this, "SEmade"+ CurrentBar.ToString(), true, 0, entry.shortEntryPrice + (TickSize * 5), Brushes.Red);
				signals[0] = -1;
				secondPivStopFlag = false;
			}
		}
		///  entry marked and rcorded next bar for pullback benifit
		public void showEntryDelay() {
			/// Long Signal Found 1 bar ago
			if ( signals[1] == 1 ) {
				
				/// if short or flat
				if (entry.inShortTrade || ( !entry.inShortTrade && ! entry.inLongTrade )) {
					/// if long entry benificial or still over entry line go long else exit
					if (Close[0] <= Close[1] || Close[0] <= Low[1] || ( High[0] > entry.longEntryPrice && Low[0] < entry.longEntryPrice )) {
						entry.inLongTrade = true;
						entry.inShortTrade = false;
						tradeData.signalName = "LE";
						entry.longEntryBarnum = CurrentBar;
						/// reset pivot data
						entry.pivStopCounter = 0;
						entry.lastPivotValue = swingData.lastLow ;
						entry.longEntryActual = Close[0];
						entry.pivLineLength = 0;
						entry.barsSinceEntry = 0;
						Draw.Dot(this, "actualLE"+CurrentBar, true, 0, Open[0], Brushes.LimeGreen);
						customDrawTrades( show: true,  simple: false);
					} else {
						exitFromGap();
						Draw.Dot(this, "SXGapDot"+CurrentBar, true, 0, Close[0], Brushes.Yellow);
						Draw.Text(this, "SXGap"+CurrentBar, "SX-Gap", 0, Open[0], Brushes.Yellow);
					}	
				}
			}
			/// Short signal found
			if ( signals[1] == -1 ) {
				//entry.inShortTrade = true;
				/// if long or flat and missed signal was short, go short
				if (entry.inLongTrade || ( !entry.inShortTrade && ! entry.inLongTrade )) {
					/// if short entry benificial or still over entry line go short else exit
					if (Close[0] >= Close[1] || Close[0] > Low[1] || ( High[0] > entry.shortEntryPrice && Low[0] < entry.shortEntryPrice ) ) {
						/// normal trade entry
						entry.inLongTrade = false;
						entry.inShortTrade = true;
						tradeData.signalName = "SE";
						entry.shortEntryBarnum = CurrentBar;
						/// reset pivot data
						entry.pivStopCounter = 0;
						entry.lastPivotValue =  swingData.lastHigh;
						entry.shortEntryActual = entry.shortEntryPrice;
						entry.pivLineLength = 0;
						entry.barsSinceEntry = 0;
						Draw.Dot(this, "actualSE"+CurrentBar, true, 0, Open[0], Brushes.Crimson);
						customDrawTrades( show: true,  simple: false);
						
					} else {
						exitFromGap();
						Draw.Dot(this, "LXGapDot"+CurrentBar, true, 0, Open[0], Brushes.Yellow);
						Draw.Text(this, "LXGap"+CurrentBar, "LX-Gap", 0, High[0], Brushes.Yellow);
					}
				}
			}
		}
		
		/// exit from adversarial gap
		public void exitFromGap() {
			if ( entry.inLongTrade  ) {
				/// need short trades to debug this no long stops hit
				entry.inLongTrade = false;
                signals[0] = 2;
				entry.longHardStopBarnum	= CurrentBar;
				tradeData.signalName = "LX - Gap";
				entry.barsSinceEntry = 0;
			} else if ( entry.inShortTrade  ) {
				/// need short trades to debug this no long stops hit
				entry.inShortTrade = false;
                signals[0] = -2;
				entry.shortHardStopBarnum	= CurrentBar;
				tradeData.signalName = "SX - HS";
				entry.barsSinceEntry = 0;
			}
		}
		
		/// looking short
		public void findShortEntry() {
			if ( swingData.lastHighBarnum > swingData.lastLowBarnum ) {
				int distanceToLow = CurrentBar - swingData.lastLowBarnum;
				int distanceToHigh = CurrentBar - swingData.lastHighBarnum;
				int lastBar = CurrentBar -1;
				double upSwingDistance = Math.Abs(swingData.lastHigh - swingData.lastLow);
				double upSwingEntry = Math.Abs(upSwingDistance * 0.382);
				entry.shortEntryPrice = Math.Abs(swingData.lastHigh - upSwingEntry);
				
				/// draw upswing in red
				RemoveDrawObject("upline"+lastBar);
				Draw.Line(this, "upline"+CurrentBar.ToString(), false, distanceToLow, swingData.lastLow, distanceToHigh, swingData.lastHigh, Brushes.DarkRed, DashStyleHelper.Dash, 2);
				/// draw entry line
				RemoveDrawObject("shortEntry"+lastBar);
				Draw.Line(this, "shortEntry"+CurrentBar.ToString(), false, distanceToLow, entry.shortEntryPrice , distanceToHigh, entry.shortEntryPrice , Brushes.Red, DashStyleHelper.Dash, 2);
				/// show swing high height
				
				double swingProfit = Math.Abs((upSwingDistance * 0.236) * 100);
			} else {
				// disable short entry
				entry.shortEntryPrice = 0;
				entry.shortLineLength = 0;
			}
		}
		
		/// looking long
		public void findLongeEntry() {
			if ( swingData.lastHighBarnum < swingData.lastLowBarnum ) {
				int distanceToHigh = CurrentBar - swingData.lastHighBarnum;
				int distanceToLow = CurrentBar - swingData.lastLowBarnum;
				int lastBar = CurrentBar -1;
				double dnSwingDistance = Math.Abs(( swingData.lastLow - swingData.lastHigh ) * -1);
				double dnSwingEntry = Math.Abs(dnSwingDistance * 0.382);
				entry.longEntryPrice = Math.Abs( swingData.lastLow + dnSwingEntry);
		
				/// draw down swing in green
				RemoveDrawObject("dnline"+lastBar);
				Draw.Line(this, "dnline"+CurrentBar.ToString(), false, distanceToHigh, swingData.lastHigh, distanceToLow, swingData.lastLow, Brushes.DarkGreen, DashStyleHelper.Dash, 2);
				/// draw entry line
				RemoveDrawObject("longEntry"+lastBar);
				Draw.Line(this, "longEntry"+CurrentBar.ToString(), false, distanceToHigh, entry.longEntryPrice, distanceToLow, entry.longEntryPrice, Brushes.LimeGreen, DashStyleHelper.Dash, 2);
				
				/// show swing low height
				double swingProfit = Math.Abs((dnSwingDistance * 0.236) * 100);				
			}	else {
				/// disable long entry
				entry.longEntryPrice = 0;
				entry.longLineLength = 0;
			}
		}
		
		/// init params
		public void resetStruct(bool doIt) {
			swingData.lastHigh 		= Low[0];
			swingData.lastHighBarnum  = 0;
			swingData.lastLow  		= Low[0];
			swingData.lastLowBarnum  	= 0;
			swingData.prevHigh  		= Low[0];
			swingData.prevHighBarnum  = 0;
			swingData.prevLow  		= Low[0];
			swingData.prevLowBarnum 	= 0;	
			entry.inShortTrade = true;
			entry.inLongTrade = true; 
		}
		
		
		#region Properies
		///  signal
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<int> Signals
		{
			get { return signals; }
		}
		
		///  inputs
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Shares", Order=1, GroupName="Parameters")]
		public int shares
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Swing Pct", Order=2, GroupName="Parameters")]
		public double swingPct
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Min Bars To Last Swing", Order=3, GroupName="Parameters")]
		public int minBarsToLastSwing
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable Hard Stop", Order=4, GroupName="Parameters")]
		public bool enableHardStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Pct Hard Stop", Order=5, GroupName="Parameters")]
		public int pctHardStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable Pivot Stop", Order=6, GroupName="Parameters")]
		public bool enablePivotStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Pivot Stop Swing Size", Order=7, GroupName="Parameters")]
		public int pivotStopSwingSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Pivot Stop Range", Order=8, GroupName="Parameters")]
		public double pivotStopPivotRange
		{ get; set; }
		
		/// Statistics
		[NinjaScriptProperty]
		[Display(Name="Show Up Count", Order=1, GroupName="Statistics")]
		public bool showUpCount
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Show Hard Stops", Order=2, GroupName="Statistics")]
		public bool showHardStops
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Show Trades On Chart", Order=3, GroupName="Statistics")]
		public bool printTradesOnChart
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Show Trades Simple", Order=4, GroupName="Statistics")]
		public bool printTradesSimple
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Send Trades To log", Order=5, GroupName="Statistics")]
		public bool printTradesTolog
		{ get; set; }
		
		#endregion
		
	}	
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MooreTechSwing02[] cacheMooreTechSwing02;
		public MooreTechSwing02 MooreTechSwing02(int shares, double swingPct, int minBarsToLastSwing, bool enableHardStop, int pctHardStop, bool enablePivotStop, int pivotStopSwingSize, double pivotStopPivotRange, bool showUpCount, bool showHardStops, bool printTradesOnChart, bool printTradesSimple, bool printTradesTolog)
		{
			return MooreTechSwing02(Input, shares, swingPct, minBarsToLastSwing, enableHardStop, pctHardStop, enablePivotStop, pivotStopSwingSize, pivotStopPivotRange, showUpCount, showHardStops, printTradesOnChart, printTradesSimple, printTradesTolog);
		}

		public MooreTechSwing02 MooreTechSwing02(ISeries<double> input, int shares, double swingPct, int minBarsToLastSwing, bool enableHardStop, int pctHardStop, bool enablePivotStop, int pivotStopSwingSize, double pivotStopPivotRange, bool showUpCount, bool showHardStops, bool printTradesOnChart, bool printTradesSimple, bool printTradesTolog)
		{
			if (cacheMooreTechSwing02 != null)
				for (int idx = 0; idx < cacheMooreTechSwing02.Length; idx++)
					if (cacheMooreTechSwing02[idx] != null && cacheMooreTechSwing02[idx].shares == shares && cacheMooreTechSwing02[idx].swingPct == swingPct && cacheMooreTechSwing02[idx].minBarsToLastSwing == minBarsToLastSwing && cacheMooreTechSwing02[idx].enableHardStop == enableHardStop && cacheMooreTechSwing02[idx].pctHardStop == pctHardStop && cacheMooreTechSwing02[idx].enablePivotStop == enablePivotStop && cacheMooreTechSwing02[idx].pivotStopSwingSize == pivotStopSwingSize && cacheMooreTechSwing02[idx].pivotStopPivotRange == pivotStopPivotRange && cacheMooreTechSwing02[idx].showUpCount == showUpCount && cacheMooreTechSwing02[idx].showHardStops == showHardStops && cacheMooreTechSwing02[idx].printTradesOnChart == printTradesOnChart && cacheMooreTechSwing02[idx].printTradesSimple == printTradesSimple && cacheMooreTechSwing02[idx].printTradesTolog == printTradesTolog && cacheMooreTechSwing02[idx].EqualsInput(input))
						return cacheMooreTechSwing02[idx];
			return CacheIndicator<MooreTechSwing02>(new MooreTechSwing02(){ shares = shares, swingPct = swingPct, minBarsToLastSwing = minBarsToLastSwing, enableHardStop = enableHardStop, pctHardStop = pctHardStop, enablePivotStop = enablePivotStop, pivotStopSwingSize = pivotStopSwingSize, pivotStopPivotRange = pivotStopPivotRange, showUpCount = showUpCount, showHardStops = showHardStops, printTradesOnChart = printTradesOnChart, printTradesSimple = printTradesSimple, printTradesTolog = printTradesTolog }, input, ref cacheMooreTechSwing02);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MooreTechSwing02 MooreTechSwing02(int shares, double swingPct, int minBarsToLastSwing, bool enableHardStop, int pctHardStop, bool enablePivotStop, int pivotStopSwingSize, double pivotStopPivotRange, bool showUpCount, bool showHardStops, bool printTradesOnChart, bool printTradesSimple, bool printTradesTolog)
		{
			return indicator.MooreTechSwing02(Input, shares, swingPct, minBarsToLastSwing, enableHardStop, pctHardStop, enablePivotStop, pivotStopSwingSize, pivotStopPivotRange, showUpCount, showHardStops, printTradesOnChart, printTradesSimple, printTradesTolog);
		}

		public Indicators.MooreTechSwing02 MooreTechSwing02(ISeries<double> input , int shares, double swingPct, int minBarsToLastSwing, bool enableHardStop, int pctHardStop, bool enablePivotStop, int pivotStopSwingSize, double pivotStopPivotRange, bool showUpCount, bool showHardStops, bool printTradesOnChart, bool printTradesSimple, bool printTradesTolog)
		{
			return indicator.MooreTechSwing02(input, shares, swingPct, minBarsToLastSwing, enableHardStop, pctHardStop, enablePivotStop, pivotStopSwingSize, pivotStopPivotRange, showUpCount, showHardStops, printTradesOnChart, printTradesSimple, printTradesTolog);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MooreTechSwing02 MooreTechSwing02(int shares, double swingPct, int minBarsToLastSwing, bool enableHardStop, int pctHardStop, bool enablePivotStop, int pivotStopSwingSize, double pivotStopPivotRange, bool showUpCount, bool showHardStops, bool printTradesOnChart, bool printTradesSimple, bool printTradesTolog)
		{
			return indicator.MooreTechSwing02(Input, shares, swingPct, minBarsToLastSwing, enableHardStop, pctHardStop, enablePivotStop, pivotStopSwingSize, pivotStopPivotRange, showUpCount, showHardStops, printTradesOnChart, printTradesSimple, printTradesTolog);
		}

		public Indicators.MooreTechSwing02 MooreTechSwing02(ISeries<double> input , int shares, double swingPct, int minBarsToLastSwing, bool enableHardStop, int pctHardStop, bool enablePivotStop, int pivotStopSwingSize, double pivotStopPivotRange, bool showUpCount, bool showHardStops, bool printTradesOnChart, bool printTradesSimple, bool printTradesTolog)
		{
			return indicator.MooreTechSwing02(input, shares, swingPct, minBarsToLastSwing, enableHardStop, pctHardStop, enablePivotStop, pivotStopSwingSize, pivotStopPivotRange, showUpCount, showHardStops, printTradesOnChart, printTradesSimple, printTradesTolog);
		}
	}
}

#endregion
