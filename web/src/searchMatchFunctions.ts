export interface PositionMarker {
    pos: number;
    len: number;
}

export type SearchMatchFunction = (lhs: string, rhs: string, markers: PositionMarker[]) => boolean;

function indexedAggresiveSearchMatchFunction(lhs: string, lhsIndex: number, rhs: string, markers: PositionMarker[]): boolean {
    if (!rhs) {
        return true;
    }

    lhs = lhs.toLowerCase();
    rhs = rhs.toLowerCase();

    for (let len = rhs.length; len >= 1; len -= 1) {
        const subWord = rhs.substring(0, len);
        const foundPos = lhs.indexOf(subWord, lhsIndex);

        if (foundPos >= 0) {
            markers.push({
                pos: foundPos,
                len: subWord.length
            });

            return indexedAggresiveSearchMatchFunction(lhs, foundPos + subWord.length, rhs.substring(len), markers);
        }
    }

    return false;
}

export function aggresiveSearchMatchFunction(lhs: string, rhs: string, markers: PositionMarker[]): boolean {
    return indexedAggresiveSearchMatchFunction(lhs, 0, rhs, markers);
}

export function containsSearchMatchFunction(lhs: string, rhs: string, markers: PositionMarker[]): boolean {
    const index = lhs.toLowerCase().indexOf(rhs.toLowerCase());

    if (index < 0) {
        return false;
    }

    markers.push({
        pos: index,
        len: rhs.length
    });

    return true;
}
