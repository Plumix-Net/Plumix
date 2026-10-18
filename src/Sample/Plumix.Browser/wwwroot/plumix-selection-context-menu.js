// The invisible DOM selection view from Flutter's _platform_selectable_region_context_menu_web.dart.
// Framework hit testing supplies the region under the mouse, so clipped/offstage/covered regions
// cannot intercept a native menu. One DOM view and one style are shared by every selection region.
let enabled = true;
let detach;

export function initialize(containerId, onMouseDown) {
    detach?.();
    const container = document.getElementById(containerId);
    if (!container) throw new Error(`Missing browser host ${containerId}`);
    let style = document.getElementById('plumix-selection-context-menu-style');
    if (!style) {
        style = document.createElement('style');
        style.id = 'plumix-selection-context-menu-style';
        style.textContent = `
.web-selectable-region-context-menu {
    color: transparent;
    user-select: text;
    -webkit-user-select: text;
    -moz-user-select: text;
    -ms-user-select: text;
    pointer-events: none;
    position: absolute;
    inset: 0;
}
.web-selectable-region-context-menu::selection { background: transparent; }`;
        document.head.append(style);
    }
    const element = document.createElement('div');
    element.classList.add('web-selectable-region-context-menu');
    element.style.width = '100%';
    element.style.height = '100%';
    element.setAttribute('aria-hidden', 'true');
    container.append(element);

    function mouseDown(event) {
        if (!enabled) return;
        const rect = container.getBoundingClientRect();
        const raw = onMouseDown(
            (event.clientX - rect.left) * container.clientWidth / rect.width,
            (event.clientY - rect.top) * container.clientHeight / rect.height,
            event.button);
        if (raw == null) return;
        event.preventDefault();
        const result = JSON.parse(raw);
        if (!result.selectText) return;
        element.innerText = result.text;
        const range = document.createRange();
        range.selectNode(element);
        const selection = window.getSelection();
        selection?.removeAllRanges();
        selection?.addRange(range);
    }
    function contextMenu(event) {
        if (!enabled) event.preventDefault();
    }
    container.addEventListener('mousedown', mouseDown);
    container.addEventListener('contextmenu', contextMenu);
    detach = () => {
        container.removeEventListener('mousedown', mouseDown);
        container.removeEventListener('contextmenu', contextMenu);
        element.remove();
    };
}

export function setEnabled(value) { enabled = value; }
