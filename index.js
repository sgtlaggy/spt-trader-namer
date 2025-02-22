const PROPERTIES = new Set(["Nickname", "Location", "Description"]);


function onChangeTheme() {
    const newTheme = event.target.value;
    const style = document.documentElement.style;
    style["color-scheme"] = newTheme;
}

function addTrader() {
    const trader = document.querySelector("#Trader").content.cloneNode(true);
    const traders = document.querySelector("#Traders");
    traders.appendChild(trader);
    return traders.lastElementChild;
}

function clearTraders() {
    document.querySelector("#Traders").replaceChildren();
}

function addProperty(trader) {
    const property = document.querySelector("#TraderProperty").content.cloneNode(true);
    const properties = trader.querySelector(".trader-properties");
    properties.appendChild(property);
    return properties.lastElementChild;
}

function setProperty(property, name, value) {
    property.querySelector("select").value = name;
    property.querySelector("input").value = value;
}

function onPropertyValueChanged() {
    const sel = event.target;
    const value = sel.value;
    const curInput = sel.nextElementSibling;
    const newInput = document.createElement(value === "Description" ? "textarea" : "input");
    newInput.value = curInput.value;
    curInput.replaceWith(newInput);
}

function loadConfig() {
    let config;
    try {
        config = JSON.parse(document.querySelector("#ConfigJsonArea").value);
    } catch {
        alert("Invalid JSON config!");
        return;
    }

    clearTraders();

    Object.entries(config).forEach(([name, properties]) => {
        const trader = addTrader();
        trader.querySelector("input").value = name;

        if ((typeof properties) === "string") {
            const prop = addProperty(trader);
            setProperty(prop, "Nickname", properties);
        } else {
            Object.entries(properties).forEach(([propName, value]) => {
                const property = addProperty(trader);
                setProperty(property, propName, value);
            });
        }
    });
}

async function onConfigUploaded() {
    const configContent = await event.target.files[0].text();
    document.querySelector("#ConfigJsonArea").value = configContent;
    loadConfig();
}

function jsonifyConfig() {
    const config = {};

    document.querySelectorAll(".trader").forEach((trader) => {
        const traderName = trader.querySelector("input").value;
        config[traderName] = {};
        trader.querySelectorAll(".trader-property").forEach((prop) => {
            const sel = prop.querySelector("select");
            const text = sel.nextElementSibling;
            config[traderName][sel.value] = text.value;
        });
    });

    document.querySelector("#ConfigJsonArea").value = JSON.stringify(config, null, 2);
}

function saveConfig() {
    jsonifyConfig();

    const blob = new Blob([document.querySelector("#ConfigJsonArea").value], {
        type: "application/json"
    });
    const url = URL.createObjectURL(blob);

    const a = document.querySelector("#Download");
    a.href = url;
    a.click()
}
