import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import App from './App'

describe('App', () => {
  it('renders the store heading and a main landmark', () => {
    render(<App />)

    expect(screen.getByRole('heading', { level: 1, name: 'Pizza Store' })).toBeInTheDocument()
    expect(screen.getByRole('main')).toBeInTheDocument()
  })
})
