import { onBeforeUnmount, watch, type Ref } from 'vue'

/**
 * Locks background scrolling while a dialog is open.
 *
 * The lock is reference counted because dialogs stack: opening "delete" from the note
 * detail view leaves two open at once. With a naive flag, closing the inner dialog would
 * restore scrolling while the outer one is still covering the page.
 */
let lockCount = 0
let previousOverflow: string | null = null

function acquire(): void {
  if (lockCount === 0) {
    previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
  }
  lockCount += 1
}

function release(): void {
  if (lockCount === 0) {
    return
  }

  lockCount -= 1
  if (lockCount === 0) {
    document.body.style.overflow = previousOverflow ?? ''
    previousOverflow = null
  }
}

export function useScrollLock(active: Ref<boolean>): void {
  let held = false

  function sync(shouldHold: boolean): void {
    if (shouldHold && !held) {
      acquire()
      held = true
    } else if (!shouldHold && held) {
      release()
      held = false
    }
  }

  watch(active, sync, { immediate: true })

  // A component unmounted while its dialog is open must not leak the lock.
  onBeforeUnmount(() => sync(false));
}

/** Exposed for tests; resets the module-level counter between cases. */
export function __resetScrollLock(): void {
  lockCount = 0
  previousOverflow = null
  document.body.style.overflow = ''
}
