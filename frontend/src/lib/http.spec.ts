import { AxiosError, AxiosHeaders } from 'axios'
import { describe, expect, it } from 'vitest'
import { toErrorMessage } from './http'

function axiosErrorWith(status: number, data: unknown): AxiosError {
  const error = new AxiosError('Request failed')
  error.response = {
    status,
    statusText: '',
    data,
    headers: new AxiosHeaders(),
    config: { headers: new AxiosHeaders() },
  }
  return error
}

describe('toErrorMessage', () => {
  it('prefers the ProblemDetails detail the API sends', () => {
    const error = axiosErrorWith(409, {
      title: 'Conflict',
      detail: 'An account with that email already exists.',
      code: 'User.EmailAlreadyRegistered',
    })

    expect(toErrorMessage(error)).toBe('An account with that email already exists.')
  })

  it('falls back to the title when there is no detail', () => {
    expect(toErrorMessage(axiosErrorWith(404, { title: 'Not found' }))).toBe('Not found')
  })

  it('surfaces the first model-validation message', () => {
    const error = axiosErrorWith(400, {
      title: 'One or more validation errors occurred.',
      errors: { Title: ['Title is required.'], Content: ['Too long.'] },
    })

    expect(toErrorMessage(error)).toBe('Title is required.')
  })

  it('explains a missing server rather than showing a raw axios message', () => {
    const error = new AxiosError('Network Error')

    expect(toErrorMessage(error)).toBe('Cannot reach the server. Is the API running?')
  })

  it('explains a timeout', () => {
    const error = new AxiosError('timeout exceeded')
    error.code = 'ECONNABORTED'

    expect(toErrorMessage(error)).toBe('The request timed out. Please try again.')
  })

  it('uses the supplied fallback when the body carries nothing usable', () => {
    expect(toErrorMessage(axiosErrorWith(500, {}), 'Could not save.')).toBe('Could not save.')
  })

  it('handles a plain Error', () => {
    expect(toErrorMessage(new Error('boom'))).toBe('boom')
  })

  it('handles a thrown non-error value', () => {
    expect(toErrorMessage('a string', 'Fallback.')).toBe('Fallback.')
  })
})
