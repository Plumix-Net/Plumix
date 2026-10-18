// C#-only infrastructure: the DOM half of the browser accessibility bridge. Plumix.Accessibility.WebSemantics
// decides each node's element, role and attributes (the web engine's lib/web_ui/lib/src/engine/semantics);
// this module creates and positions the elements, mirrors scroll offsets, owns the "Enable accessibility"
// placeholder and the announcement live regions, and reports user actions back to .NET.

const elements = new Map();
let host = null;
let placeholder = null;
let announcementHost = null;
let onAction = null;
let onEnable = null;
let rootId = null;
let devicePixelRatio = 1;
let requestingFocus = false;
let announcementToggle = false;

export function initialize(containerId, actionCallback, enableCallback) {
    if (host !== null) {
        return;
    }

    onAction = actionCallback;
    onEnable = enableCallback;
    const container = document.getElementById(containerId) ?? document.body;
    if (getComputedStyle(container).position === 'static') {
        container.style.position = 'relative';
    }

    host = document.createElement('flt-semantics-host');
    Object.assign(host.style, {
        position: 'absolute',
        left: '0px',
        top: '0px',
        transformOrigin: '0 0 0',
        pointerEvents: 'none',
    });
    container.appendChild(host);
    createPlaceholder();
    createAnnouncementHost();
}

// The desktop semantics enabler: a 1x1 button just off screen that a screen reader finds first.
function createPlaceholder() {
    placeholder = document.createElement('flt-semantics-placeholder');
    placeholder.setAttribute('role', 'button');
    placeholder.setAttribute('aria-live', 'polite');
    placeholder.setAttribute('tabindex', '0');
    placeholder.setAttribute('aria-label', 'Enable accessibility');
    Object.assign(placeholder.style, {
        position: 'absolute',
        left: '-1px',
        top: '-1px',
        width: '1px',
        height: '1px',
    });
    const activate = (event) => {
        if ((event.type === 'keydown' || event.type === 'keyup') && event.key !== 'Enter' && event.key !== ' ') {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        removePlaceholder();
        onEnable();
    };
    for (const type of ['click', 'keyup', 'keydown', 'mouseup', 'mousedown', 'pointerdown', 'pointerup']) {
        placeholder.addEventListener(type, activate);
    }

    document.body.prepend(placeholder);
}

export function removePlaceholder() {
    placeholder?.remove();
    placeholder = null;
}

function createAnnouncementHost() {
    announcementHost = document.createElement('flt-announcement-host');
    for (const assertiveness of ['polite', 'assertive']) {
        const region = document.createElement('flt-announcement-' + assertiveness);
        region.setAttribute('aria-live', assertiveness);
        Object.assign(region.style, {
            position: 'fixed',
            overflow: 'hidden',
            transform: 'translate(-99999px, -99999px)',
            width: '1px',
            height: '1px',
        });
        announcementHost.appendChild(region);
    }

    document.body.appendChild(announcementHost);
}

// The web engine's AccessibilityAnnouncements: the text is set on the next tick and cleared after
// 300ms; every other message gets a no-break space so a repeat is still announced.
export function announce(message, assertive) {
    const region = announcementHost.children[assertive ? 1 : 0];
    announcementToggle = !announcementToggle;
    const text = announcementToggle ? message + ' ' : message;
    setTimeout(() => {
        region.textContent = text;
        setTimeout(() => { region.textContent = ''; }, 300);
    }, 0);
}

export function clear() {
    for (const entry of elements.values()) {
        entry.element.remove();
    }

    elements.clear();
    rootId = null;
}

export function update(json) {
    const data = JSON.parse(json);
    devicePixelRatio = data.devicePixelRatio || 1;
    host.style.transform = `scale(${1 / devicePixelRatio})`;
    for (const id of data.removed) {
        const entry = elements.get(id);
        if (entry) {
            entry.element.remove();
            elements.delete(id);
        }
    }

    for (const node of data.nodes) {
        applyNode(node);
    }

    for (const node of data.nodes) {
        applyChildren(elements.get(node.id));
    }

    rootId = data.root;
    const root = rootId === null ? null : elements.get(rootId);
    if (root && root.element.parentElement !== host) {
        host.replaceChildren(root.element);
    } else if (!root) {
        host.replaceChildren();
    }

    if (root) {
        // The semantics tree is invisible; only assistive technologies read it.
        root.element.style.filter = 'opacity(0%)';
        root.element.style.color = 'rgba(0,0,0,0)';
        positionSubtree(root, 0, 0);
    }

    for (const node of data.nodes) {
        if (node.focused) {
            const entry = elements.get(node.id);
            const target = entry?.rangeInput ?? entry?.element;
            if (target && document.activeElement !== target) {
                requestingFocus = true;
                target.focus({ preventScroll: true });
                requestingFocus = false;
            }
        }
    }
}

function applyNode(node) {
    let entry = elements.get(node.id);
    if (!entry || entry.node.tag !== node.tag) {
        const element = document.createElement(node.tag);
        element.style.position = 'absolute';
        element.style.overflow = 'visible';
        element.style.pointerEvents = 'none';
        if (node.tag === 'a') {
            element.style.display = 'block';
        } else if (/^h[1-6]$/.test(node.tag)) {
            Object.assign(element.style, { margin: '0px', padding: '0px', fontSize: '10px' });
        }

        const replaced = entry;
        entry = { element, node, attributes: new Set(), label: null, image: null, rangeInput: null, overflow: null };
        attachListeners(entry);
        if (replaced) {
            for (const childId of replaced.node.children) {
                const child = elements.get(childId);
                if (child) {
                    element.appendChild(child.element);
                }
            }

            replaced.element.replaceWith(element);
        }

        elements.set(node.id, entry);
    }

    entry.node = node;
    const element = entry.element;
    for (const name of entry.attributes) {
        if (!(name in node.attrs)) {
            element.removeAttribute(name);
        }
    }

    entry.attributes = new Set(Object.keys(node.attrs));
    for (const [name, value] of Object.entries(node.attrs)) {
        if (element.getAttribute(name) !== value) {
            element.setAttribute(name, value);
        }
    }

    if (node.focusable) {
        element.tabIndex = 0;
    } else {
        element.removeAttribute('tabindex');
    }

    applyLabel(entry, node);
    applyImage(entry, node);
    applyRange(entry, node);
    applyScroll(entry, node);
}

function attachListeners(entry) {
    entry.element.addEventListener('click', (event) => {
        if (entry.node.tappable) {
            event.stopPropagation();
            onAction(entry.node.id, 'tap', '');
        }
    });
    entry.element.addEventListener('focus', () => {
        if (!requestingFocus && entry.node.focusable) {
            onAction(entry.node.id, 'focus', '');
        }
    });
}

// LabelAndValue: domText is a text node, sizedSpan a span; aria-label arrives as an attribute.
function applyLabel(entry, node) {
    if (node.text === undefined) {
        entry.label?.remove();
        entry.label = null;
        return;
    }

    const wantsSpan = node.textAsSpan === true;
    const isSpan = entry.label instanceof HTMLElement;
    if (!entry.label || wantsSpan !== isSpan) {
        entry.label?.remove();
        if (wantsSpan) {
            entry.label = document.createElement('span');
            Object.assign(entry.label.style, {
                display: 'inline-block',
                whiteSpace: 'nowrap',
                transformOrigin: '0 0 0',
                pointerEvents: 'none',
            });
        } else {
            entry.label = document.createTextNode('');
        }

        entry.element.prepend(entry.label);
    }

    entry.label.textContent = node.text;
}

function applyImage(entry, node) {
    if (node.imageLabel === undefined) {
        entry.image?.remove();
        entry.image = null;
        return;
    }

    if (!entry.image) {
        entry.image = document.createElement('flt-semantics-img');
        entry.image.setAttribute('role', 'img');
        entry.image.style.position = 'absolute';
        entry.image.style.top = '0px';
        entry.image.style.left = '0px';
        entry.element.prepend(entry.image);
    }

    entry.image.setAttribute('aria-label', node.imageLabel);
    entry.image.style.width = node.rect[2] + 'px';
    entry.image.style.height = node.rect[3] + 'px';
}

// SemanticIncrementable: a range input around a surrogate value; moving it increases or decreases.
function applyRange(entry, node) {
    if (!node.range) {
        entry.rangeInput?.remove();
        entry.rangeInput = null;
        return;
    }

    if (!entry.rangeInput) {
        const input = document.createElement('input');
        input.type = 'range';
        input.setAttribute('role', 'slider');
        Object.assign(input.style, { position: 'absolute', inset: '0px', width: '100%', appearance: 'none' });
        input.addEventListener('change', () => {
            if (input.disabled) {
                return;
            }

            const next = parseInt(input.value, 10);
            if (next > entry.surrogate) {
                entry.surrogate += 1;
                onAction(entry.node.id, 'increase', '');
            } else if (next < entry.surrogate) {
                entry.surrogate -= 1;
                onAction(entry.node.id, 'decrease', '');
            }
        });
        input.addEventListener('focus', () => {
            if (!requestingFocus) {
                onAction(entry.node.id, 'focus', '');
            }
        });
        entry.rangeInput = input;
        entry.surrogate = 1;
        entry.element.appendChild(input);
    }

    const input = entry.rangeInput;
    const surrogate = entry.surrogate;
    input.disabled = !node.range.enabled;
    input.value = String(surrogate);
    input.setAttribute('aria-valuenow', String(surrogate));
    input.setAttribute('aria-valuetext', node.range.value);
    const max = node.range.canIncrease ? surrogate + 1 : surrogate;
    const min = node.range.canDecrease ? surrogate - 1 : surrogate;
    input.max = String(max);
    input.min = String(min);
    input.setAttribute('aria-valuemax', String(max));
    input.setAttribute('aria-valuemin', String(min));
}

// SemanticScrollable: the element scrolls over an overflow child as tall (or wide) as the scroll
// extent, its offset mirrors the framework's position, and a scroll the user (or a screen reader)
// makes is reported as scrollToOffset.
function applyScroll(entry, node) {
    const element = entry.element;
    if (!node.scroll || node.scroll.axis === 'none') {
        entry.overflow?.remove();
        entry.overflow = null;
        element.style.overflow = 'visible';
        entry.scrollLeft = 0;
        entry.scrollTop = 0;
        return;
    }

    const vertical = node.scroll.axis === 'vertical';
    if (!entry.overflow) {
        entry.overflow = document.createElement('flt-semantics-scroll-overflow');
        Object.assign(entry.overflow.style, { position: 'absolute', pointerEvents: 'none' });
        element.prepend(entry.overflow);
        element.addEventListener('scroll', () => {
            const position = vertical ? Math.round(element.scrollTop) : Math.round(element.scrollLeft);
            if (position === entry.expectedScroll) {
                return;
            }

            entry.expectedScroll = position;
            onAction(entry.node.id, 'scrollToOffset', vertical ? `0,${position}` : `${position},0`);
        });
    }

    Object.assign(element.style, {
        overflowX: vertical ? 'hidden' : 'scroll',
        overflowY: vertical ? 'scroll' : 'hidden',
        touchAction: 'none',
        scrollbarWidth: 'none',
    });
    const extent = node.scroll.extentMax - node.scroll.extentMin;
    if (vertical) {
        entry.overflow.style.width = '1px';
        entry.overflow.style.height = (extent + node.rect[3]).toFixed(1) + 'px';
    } else {
        entry.overflow.style.width = (extent + node.rect[2]).toFixed(1) + 'px';
        entry.overflow.style.height = '1px';
    }

    entry.pendingScroll = Math.round(node.scroll.position);
}

function applyChildren(entry) {
    if (!entry) {
        return;
    }

    const childIds = entry.node.children;
    for (const childId of childIds) {
        const child = elements.get(childId);
        if (child) {
            entry.element.appendChild(child.element);
        }
    }

    // DOM order is traversal order; z-order is the reversed hit-test order.
    if (childIds.length > 1) {
        entry.node.hitTest.forEach((childId, index) => {
            const child = elements.get(childId);
            if (child) {
                child.element.style.zIndex = String(childIds.length - index);
            }
        });
    }
}

// recomputePositionAndSize: a child is placed relative to its parent's rect origin, shifted by the
// parent's scroll offset, under its own transform.
function positionSubtree(entry, horizontalAdjustment, verticalAdjustment) {
    const element = entry.element;
    const [rectLeft, rectTop, width, height] = entry.node.rect;
    element.style.width = width + 'px';
    element.style.height = height + 'px';
    const left = rectLeft + horizontalAdjustment;
    const top = rectTop + verticalAdjustment;
    const transform = entry.node.transform;
    if (transform) {
        const m = transform.slice();
        m[12] = transform[0] * left + transform[4] * top + transform[12];
        m[13] = transform[1] * left + transform[5] * top + transform[13];
        m[14] = transform[2] * left + transform[6] * top + transform[14];
        m[15] = transform[3] * left + transform[7] * top + transform[15];
        element.style.transformOrigin = '0 0 0';
        element.style.transform = `matrix3d(${m.join(',')})`;
    } else if (left !== 0 || top !== 0) {
        element.style.transformOrigin = '0 0 0';
        element.style.transform = `translate(${left}px, ${top}px)`;
    } else {
        element.style.removeProperty('transform');
        element.style.removeProperty('transform-origin');
    }

    let scrollLeft = 0;
    let scrollTop = 0;
    if (entry.overflow && entry.pendingScroll !== undefined) {
        const vertical = entry.node.scroll.axis === 'vertical';
        entry.expectedScroll = entry.pendingScroll;
        if (vertical) {
            element.scrollTop = entry.pendingScroll;
            scrollTop = element.scrollTop;
        } else {
            element.scrollLeft = entry.pendingScroll;
            scrollLeft = element.scrollLeft;
        }
    }

    for (const childId of entry.node.children) {
        const child = elements.get(childId);
        if (child) {
            positionSubtree(child, -rectLeft + scrollLeft, -rectTop + scrollTop);
        }
    }
}
