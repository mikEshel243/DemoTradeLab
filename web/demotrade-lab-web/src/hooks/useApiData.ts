import { useEffect, useState } from 'react'
import { getOverview, getTrade, listTrades } from '../api/client'
import type { OverviewData, Trade, TradeFilters } from '../api/types'

export interface Loadable<T> {
  data: T | null
  error: string | null
  isLoading: boolean
}

interface Reloadable<T> extends Loadable<T> {
  reload: () => void
}

const initialLoadable = <T,>(): Loadable<T> => ({
  data: null,
  error: null,
  isLoading: true,
})

export function useOverview(): Reloadable<OverviewData> {
  const [state, setState] = useState<Loadable<OverviewData>>(
    initialLoadable<OverviewData>,
  )
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setState((current) => ({ ...current, error: null, isLoading: true }))

    getOverview(controller.signal)
      .then((data) => setState({ data, error: null, isLoading: false }))
      .catch((error: unknown) => {
        if (!isAbortError(error)) {
          setState({ data: null, error: toMessage(error), isLoading: false })
        }
      })

    return () => controller.abort()
  }, [reloadToken])

  return {
    ...state,
    reload: () => setReloadToken((value) => value + 1),
  }
}

export function useTrades(filters: TradeFilters): Reloadable<Trade[]> {
  const [state, setState] = useState<Loadable<Trade[]>>(initialLoadable<Trade[]>)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setState((current) => ({ ...current, error: null, isLoading: true }))

    listTrades(filters, controller.signal)
      .then((data) => setState({ data, error: null, isLoading: false }))
      .catch((error: unknown) => {
        if (!isAbortError(error)) {
          setState({ data: null, error: toMessage(error), isLoading: false })
        }
      })

    return () => controller.abort()
  }, [filters, reloadToken])

  return {
    ...state,
    reload: () => setReloadToken((value) => value + 1),
  }
}

export function useTradeDetails(id: string | null): Loadable<Trade> {
  const [state, setState] = useState<Loadable<Trade>>({
    data: null,
    error: null,
    isLoading: false,
  })

  useEffect(() => {
    if (!id) return

    const controller = new AbortController()
    setState({ data: null, error: null, isLoading: true })

    getTrade(id, controller.signal)
      .then((data) => setState({ data, error: null, isLoading: false }))
      .catch((error: unknown) => {
        if (!isAbortError(error)) {
          setState({ data: null, error: toMessage(error), isLoading: false })
        }
      })

    return () => controller.abort()
  }, [id])

  return id ? state : { data: null, error: null, isLoading: false }
}

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}

function toMessage(error: unknown): string {
  return error instanceof Error
    ? error.message
    : 'An unexpected client error occurred.'
}
