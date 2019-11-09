export interface ITabInfo {
    button: HTMLInputElement,
    content: HTMLInputElement
}

export class TabControl {
    public constructor(private tabs: ITabInfo[]) {
        let tabInfo: ITabInfo;
        for (tabInfo of this.tabs) {
            const localTabInfo: ITabInfo = tabInfo;
            localTabInfo.button.addEventListener('click', () => {
                this.setActiveTab(localTabInfo);
            });
        }

        this.setActiveTab(tabs[0]);
    }

    private setActiveTab(activeTabInfo: ITabInfo) {
        let tabInfo: ITabInfo;

        for (tabInfo of this.tabs) {
            tabInfo.button.style.removeProperty('font-weight');
            tabInfo.button.style.setProperty('color', '#C0C0C0');
            tabInfo.content.style.setProperty('display', 'none');
        }

        activeTabInfo.button.style.setProperty('font-weight', 'bold');
        activeTabInfo.button.style.removeProperty('color');
        activeTabInfo.content.style.removeProperty('display');
    }
}
