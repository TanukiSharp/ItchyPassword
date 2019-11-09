export interface ITabInfo {
    button: HTMLInputElement,
    content: HTMLInputElement
}

export class TabControl {
    private _activeTabIndex: number = -1;

    public get activeTabIndex(): number {
        return this._activeTabIndex;
    }
    public set activeTabIndex(index: number) {
        if (index < 0 || index >= this.tabs.length) {
            throw new Error(`Argument 'index' out of range. Must be in range [0;${this.tabs.length - 1}].`);
        }
        this.setActiveTab(index);
    }

    public constructor(private tabs: ITabInfo[]) {
        for (let i = 0; i < this.tabs.length; i += 1) {
            tabs[i].button.addEventListener('click', () => {
                this.setActiveTab(i);
            });
        }

        this.setActiveTab(0);
    }

    private setActiveTab(activeTabIndex: number) {
        let tabInfo: ITabInfo;

        for (tabInfo of this.tabs) {
            tabInfo.button.style.removeProperty('font-weight');
            tabInfo.button.style.setProperty('color', '#C0C0C0');
            tabInfo.content.style.setProperty('display', 'none');
        }

        this.tabs[activeTabIndex].button.style.setProperty('font-weight', 'bold');
        this.tabs[activeTabIndex].button.style.removeProperty('color');
        this.tabs[activeTabIndex].content.style.removeProperty('display');

        this._activeTabIndex = activeTabIndex;
    }
}
