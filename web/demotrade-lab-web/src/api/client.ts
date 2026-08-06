import type {
  CurrencyProfitLossTimeline,
  DashboardAnalytics,
  InstrumentSummary,
  OverviewData,
  Trade,
  TradeFilters,
} from './types'

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

async function getJson<T>(path: string, signal: AbortSignal): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: { Accept: 'application/json' },
    signal,
  })

  if (!response.ok) {
    throw new Error(await readProblem(response))
  }

  return (await response.json()) as T
}

async function readProblem(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as ProblemDetails
    const validationMessages = problem.errors
      ? Object.values(problem.errors).flat().join(' ')
      : ''

    return (
      validationMessages ||
      problem.detail ||
      problem.title ||
      `The API returned HTTP ${response.status}.`
    )
  } catch {
    return `The API returned HTTP ${response.status}.`
  }
}

export async function getOverview(signal: AbortSignal): Promise<OverviewData> {
  const [dashboard, instruments, timelines] = await Promise.all([
    getJson<DashboardAnalytics>('/api/analytics/dashboard', signal),
    getJson<InstrumentSummary[]>('/api/analytics/instruments', signal),
    getJson<CurrencyProfitLossTimeline[]>(
      '/api/analytics/profit-loss-timeline',
      signal,
    ),
  ])

  return { dashboard, instruments, timelines }
}

export function listTrades(
  filters: TradeFilters,
  signal: AbortSignal,
): Promise<Trade[]> {
  const query = new URLSearchParams()

  if (filters.instrument) query.set('instrument', filters.instrument)
  if (filters.currency) query.set('currency', filters.currency)
  if (filters.direction) query.set('direction', filters.direction)
  if (filters.outcome) query.set('outcome', filters.outcome)

  query.set('sortBy', filters.sortBy)
  query.set('sortDirection', filters.sortDirection)

  return getJson<Trade[]>(`/api/trades?${query.toString()}`, signal)
}

export function getTrade(id: string, signal: AbortSignal): Promise<Trade> {
  return getJson<Trade>(`/api/trades/${encodeURIComponent(id)}`, signal)
}
