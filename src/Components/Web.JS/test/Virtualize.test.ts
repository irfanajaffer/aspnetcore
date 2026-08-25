import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import { Virtualize } from '../src/Virtualize';

const SpacerVisibilityReason = {
  UserScroll: 0,
  ProgrammaticScroll: 1,
  ViewportFill: 2,
} as const;

class TestIntersectionObserver {
  static instance: TestIntersectionObserver;

  constructor(private readonly callback: IntersectionObserverCallback) {
    TestIntersectionObserver.instance = this;
  }

  observe(): void { }
  unobserve(): void { }
  disconnect(): void { }

  trigger(target: Element, isIntersecting: boolean): void {
    this.callback([{
      target,
      isIntersecting,
      rootBounds: { height: 100 },
      boundingClientRect: { top: 0, bottom: 10 },
      intersectionRect: { top: 0, bottom: 10 },
    } as IntersectionObserverEntry], this as unknown as IntersectionObserver);
  }
}

class TestResizeObserver {
  observe(): void { }
  unobserve(): void { }
  disconnect(): void { }
}

class TestMutationObserver {
  observe(): void { }
  disconnect(): void { }
  takeRecords(): MutationRecord[] { return []; }
}

function createVirtualize(): {
  dotNetHelper: any;
  scrollContainer: HTMLElement;
  spacerBefore: HTMLElement;
  invokeMethodAsync: jest.Mock;
} {
  const scrollContainer = document.createElement('div');
  scrollContainer.style.overflowY = 'auto';
  const spacerBefore = document.createElement('div');
  const item = document.createElement('div');
  const spacerAfter = document.createElement('div');
  spacerBefore.style.overflowY = 'visible';
  item.style.overflowY = 'visible';
  spacerAfter.style.overflowY = 'visible';
  scrollContainer.append(spacerBefore, item, spacerAfter);
  document.body.append(scrollContainer);

  const invokeMethodAsync = jest.fn();
  const dotNetHelper: any = {
    _callDispatcher: {},
    _id: 1,
    invokeMethodAsync,
    dispose: jest.fn(),
  };

  Virtualize.init(dotNetHelper, spacerBefore, spacerAfter, 0);
  return { dotNetHelper, scrollContainer, spacerBefore, invokeMethodAsync };
}

beforeEach(() => {
  jest.useFakeTimers();
  Object.defineProperty(globalThis, 'IntersectionObserver', { configurable: true, value: TestIntersectionObserver });
  Object.defineProperty(globalThis, 'ResizeObserver', { configurable: true, value: TestResizeObserver });
  Object.defineProperty(globalThis, 'MutationObserver', { configurable: true, value: TestMutationObserver });
  Object.defineProperty(globalThis, 'CSS', { configurable: true, value: { supports: () => true } });
  Object.defineProperty(document, 'createRange', {
    configurable: true,
    value: () => ({
      setStartAfter: () => { },
      setEndBefore: () => { },
      getBoundingClientRect: () => ({ height: 100 }),
    }),
  });
});

afterEach(() => {
  document.body.replaceChildren();
  jest.useRealTimers();
});

describe('Virtualize exports', () => {
  test('exports expected functions', () => {
    expect(typeof Virtualize.init).toBe('function');
    expect(typeof Virtualize.dispose).toBe('function');
    expect(typeof Virtualize.scrollToBottom).toBe('function');
    expect(typeof Virtualize.refreshObservers).toBe('function');
    expect(typeof Virtualize.setAnchorMode).toBe('function');
    expect(typeof Virtualize.restoreAnchor).toBe('function');
  });
});

describe('Virtualize intersection ownership', () => {
  test('same spacer uses latest ownership when coalesced during throttle', () => {
    const { dotNetHelper, scrollContainer, spacerBefore, invokeMethodAsync } = createVirtualize();
    const observer = TestIntersectionObserver.instance;

    observer.trigger(spacerBefore, true);
    invokeMethodAsync.mockClear();

    Virtualize.beginProgrammaticScroll(dotNetHelper);
    observer.trigger(spacerBefore, true);
    scrollContainer.dispatchEvent(new WheelEvent('wheel'));
    observer.trigger(spacerBefore, true);
    jest.advanceTimersByTime(50);

    expect(invokeMethodAsync).toHaveBeenCalledTimes(1);
    expect(invokeMethodAsync.mock.calls[0][4]).toBe(SpacerVisibilityReason.UserScroll);
  });

  test('non-intersecting alignment entry releases current ownership', () => {
    const { dotNetHelper, spacerBefore, invokeMethodAsync } = createVirtualize();
    const observer = TestIntersectionObserver.instance;

    observer.trigger(spacerBefore, true);
    invokeMethodAsync.mockClear();

    Virtualize.beginProgrammaticScroll(dotNetHelper);
    observer.trigger(spacerBefore, false);
    jest.advanceTimersByTime(50);
    observer.trigger(spacerBefore, true);

    expect(invokeMethodAsync).toHaveBeenCalledTimes(1);
    expect(invokeMethodAsync.mock.calls[0][4]).toBe(SpacerVisibilityReason.ViewportFill);
  });

  test('stale generation does not clear ownership of a newer overlapping scroll', () => {
    const { dotNetHelper, spacerBefore, invokeMethodAsync } = createVirtualize();
    const observer = TestIntersectionObserver.instance;

    observer.trigger(spacerBefore, true);
    invokeMethodAsync.mockClear();

    Virtualize.beginProgrammaticScroll(dotNetHelper);
    invokeMethodAsync.mockImplementationOnce(() => Virtualize.beginProgrammaticScroll(dotNetHelper));
    observer.trigger(spacerBefore, true);
    jest.advanceTimersByTime(50);

    expect(invokeMethodAsync).toHaveBeenCalledTimes(1);
    expect(invokeMethodAsync.mock.calls[0][4]).toBe(SpacerVisibilityReason.ProgrammaticScroll);

    invokeMethodAsync.mockClear();
    observer.trigger(spacerBefore, true);

    expect(invokeMethodAsync).toHaveBeenCalledTimes(1);
    expect(invokeMethodAsync.mock.calls[0][4]).toBe(SpacerVisibilityReason.ProgrammaticScroll);

    invokeMethodAsync.mockClear();
    observer.trigger(spacerBefore, true);
    jest.advanceTimersByTime(50);

    expect(invokeMethodAsync).toHaveBeenCalledTimes(1);
    expect(invokeMethodAsync.mock.calls[0][4]).toBe(SpacerVisibilityReason.ViewportFill);
  });
});
