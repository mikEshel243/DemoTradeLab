export type TradeDirection = 'buy' | 'sell'
export type TradeDataSource = 'manual' | 'sample' | 'imported'
export type TradeOutcome = 'profitable' | 'losing' | 'breakEven'
export type TradeSortField =
  | 'closedAtUtc'
  | 'openedAtUtc'
  | 'instrument'
  | 'realizedProfitLoss'
  | 'duration'
export type TradeSortDirection = 'ascending' | 'descending'

export interface Trade {
  id: string
  instrument: string
  direction: TradeDirection
  openedAtUtc: string
  closedAtUtc: string
  openingPrice: number
  closingPrice: number
  quantity: number
  realizedProfitLoss: number
  currency: string
  fees: number | null
  financingCosts: number | null
  source: TradeDataSource
  importedAtUtc: string | null
}

export interface TradeHighlight {
  id: string
  instrument: string
  closedAtUtc: string
  realizedProfitLoss: number
}

export interface CurrencyPerformance {
  currency: string
  totalRealizedProfitLoss: number
  bestTrade: TradeHighlight
  worstTrade: TradeHighlight
}

export interface DashboardAnalytics {
  totalTrades: number
  profitableTrades: number
  losingTrades: number
  breakEvenTrades: number
  winRatePercentage: number
  mostActiveInstrument: string | null
  averageTradeDurationMinutes: number | null
  currencyPerformance: CurrencyPerformance[]
}

export interface InstrumentSummary {
  instrument: string
  currency: string
  totalTrades: number
  profitableTrades: number
  losingTrades: number
  breakEvenTrades: number
  winRatePercentage: number
  totalRealizedProfitLoss: number
  averageTradeDurationMinutes: number
}

export interface ProfitLossPoint {
  tradeId: string
  instrument: string
  closedAtUtc: string
  realizedProfitLoss: number
  cumulativeRealizedProfitLoss: number
}

export interface CurrencyProfitLossTimeline {
  currency: string
  points: ProfitLossPoint[]
}

export interface TradeFilters {
  instrument: string
  currency: string
  direction: '' | TradeDirection
  outcome: '' | TradeOutcome
  sortBy: TradeSortField
  sortDirection: TradeSortDirection
}

export interface OverviewData {
  dashboard: DashboardAnalytics
  instruments: InstrumentSummary[]
  timelines: CurrencyProfitLossTimeline[]
}
