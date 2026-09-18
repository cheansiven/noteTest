import { nextTick, onBeforeUnmount, watch, type Ref } from 'vue'

const FOCUSABLE = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',')

/**
 * Deliberately not `offsetParent !== null`: that reads as hidden for any
 * position:fixed element, which is exactly what a dialog is.
 */
function isVisible(element: HTMLElement): boolean {
  if (element.hasAttribute('hidden')) {
    return false
  }

  const style = getComputedStyle(element)
  return style.display !== 'none' && style.visibility !== 'hidden'
}

function focusableWithin(container: HTMLElement): HTMLElement[] {
  return Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE)).filter(isVisible)
}

/**
 * Keeps keyboard focus inside an open dialog and hands it back afterwards.
 *
 * Setting initial focus is not enough on its own: without trapping, Tab walks straight
 * out of the dialog and into the page behind it, which for a screen-reader or
 * keyboard-only user means silently operating a UI they cannot see.
 */
export function useFocusTrap(container: Ref<HTMLElement | null>, active: Ref<boolean>): void {
  let previouslyFocused: HTMLElement | null = null

  function onKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Tab' || !container.value) {
      return
    }

    const focusable = focusableWithin(container.value)
    if (focusable.length === 0) {
      // Nothing to focus: keep focus on the dialog itself rather than losing it.
      event.preventDefault()
      return
    }

    const first = focusable[0]
    const last = focusable[focusable.length - 1]
    const current = document.activeElement

    if (!container.value.contains(current)) {
      event.preventDefault()
      first.focus()
      return
    }

    // Wrap around at both ends so focus cycles within the dialog.
    if (event.shiftKey && current === first) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && current === last) {
      event.preventDefault()
      first.focus()
    }
  }

  watch(
    active,
    async (isActive) => {
      if (isActive) {
        previouslyFocused = document.activeElement as HTMLElement | null
        document.addEventListener('keydown', onKeydown, true)

        await nextTick()
        const target =
          container.value?.querySelector<HTMLElement>('[data-autofocus]') ??
          (container.value ? focusableWithin(container.value)[0] : null)
        target?.focus()
      } else {
        document.removeEventListener('keydown', onKeydown, true)
        // Return focus to whatever opened the dialog, so the user does not get
        // dumped back at the top of the document.
        previouslyFocused?.focus()
        previouslyFocused = null
      }
    },
    { immediate: true },
  )

  onBeforeUnmount(() => document.removeEventListener('keydown', onKeydown, true))
}
