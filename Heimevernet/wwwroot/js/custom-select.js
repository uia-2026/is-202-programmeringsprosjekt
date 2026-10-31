// Progressive-enhancement custom dropdown for .filter-bar selects.
// Keeps the real <select> in the DOM (hidden, but still the thing that
// submits the form) and layers a navy-styled button + listbox on top of it,
// so no Razor markup or controller code has to change.
(function () {
    function enhance(select) {
        if (select.dataset.enhanced) return;
        select.dataset.enhanced = "true";

        var wrapper = document.createElement("div");
        wrapper.className = "custom-select";
        if (select.classList.contains("filter-bar-wide")) {
            wrapper.classList.add("custom-select-wide");
        }

        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(select);
        select.classList.add("custom-select-native");
        select.setAttribute("aria-hidden", "true");
        select.setAttribute("tabindex", "-1");

        var trigger = document.createElement("button");
        trigger.type = "button";
        trigger.className = "custom-select-trigger";
        trigger.setAttribute("aria-haspopup", "listbox");
        trigger.setAttribute("aria-expanded", "false");

        var label = document.createElement("span");
        label.className = "custom-select-label";
        trigger.appendChild(label);

        var chevron = document.createElement("span");
        chevron.className = "custom-select-chevron";
        chevron.setAttribute("aria-hidden", "true");
        chevron.textContent = "\u25BE";
        trigger.appendChild(chevron);

        var panel = document.createElement("ul");
        panel.className = "custom-select-panel";
        panel.setAttribute("role", "listbox");

        function buildOptions() {
            panel.innerHTML = "";
            Array.prototype.forEach.call(select.options, function (opt, i) {
                var li = document.createElement("li");
                li.className = "custom-select-option";
                li.setAttribute("role", "option");
                li.dataset.index = i;
                li.textContent = opt.textContent;
                if (opt.selected) {
                    li.classList.add("is-selected");
                    li.setAttribute("aria-selected", "true");
                }
                panel.appendChild(li);
            });
        }
        buildOptions();

        wrapper.appendChild(trigger);
        wrapper.appendChild(panel);

        function syncLabel() {
            var opt = select.options[select.selectedIndex];
            label.textContent = opt ? opt.textContent : "";
        }
        syncLabel();

        function closePanel() {
            wrapper.classList.remove("is-open");
            trigger.setAttribute("aria-expanded", "false");
        }

        function openPanel() {
            document.querySelectorAll(".custom-select.is-open").forEach(function (w) {
                if (w !== wrapper) w.classList.remove("is-open");
            });
            wrapper.classList.add("is-open");
            trigger.setAttribute("aria-expanded", "true");
            var active = panel.querySelector(".is-selected");
            if (active) active.scrollIntoView({ block: "nearest" });
        }

        trigger.addEventListener("click", function () {
            if (wrapper.classList.contains("is-open")) {
                closePanel();
            } else {
                openPanel();
            }
        });

        panel.addEventListener("click", function (e) {
            var li = e.target.closest(".custom-select-option");
            if (!li) return;

            var idx = Number(li.dataset.index);
            select.selectedIndex = idx;

            panel.querySelectorAll(".custom-select-option").forEach(function (o) {
                o.classList.remove("is-selected");
                o.removeAttribute("aria-selected");
            });
            li.classList.add("is-selected");
            li.setAttribute("aria-selected", "true");

            syncLabel();
            closePanel();
            select.dispatchEvent(new Event("change", { bubbles: true }));
        });

        trigger.addEventListener("keydown", function (e) {
            if (e.key === "Enter" || e.key === " " || e.key === "ArrowDown") {
                e.preventDefault();
                openPanel();
            } else if (e.key === "Escape") {
                closePanel();
            }
        });

        document.addEventListener("click", function (e) {
            if (!wrapper.contains(e.target)) closePanel();
        });

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") closePanel();
        });
    }

    function enhanceAll() {
        document.querySelectorAll(".filter-bar select.form-select").forEach(enhance);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", enhanceAll);
    } else {
        enhanceAll();
    }
})();