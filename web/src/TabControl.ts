export interface ITabInfo {
    getTabButton(): HTMLButtonElement;
    getTabContent(): HTMLElement;
    onTabSelected(): void;
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
            tabs[i].getTabButton().addEventListener('click', () => {
                this.setActiveTab(i);
            });
        }

        this.setActiveTab(0);
    }

    private setActiveTab(activeTabIndex: number) {
        if (activeTabIndex === this._activeTabIndex) {
            return;
        }

        let tabInfo: ITabInfo;

        for (tabInfo of this.tabs) {
            const button = tabInfo.getTabButton();
            button.style.removeProperty('font-weight');
            button.style.setProperty('color', '#C0C0C0');

            tabInfo.getTabContent().style.setProperty('display', 'none');
        }

        const button = this.tabs[activeTabIndex].getTabButton();
        button.style.setProperty('font-weight', 'bold');
        button.style.removeProperty('color');

        this.tabs[activeTabIndex].getTabContent().style.removeProperty('display');

        this._activeTabIndex = activeTabIndex;

        this.tabs[activeTabIndex].onTabSelected();
    }
}
