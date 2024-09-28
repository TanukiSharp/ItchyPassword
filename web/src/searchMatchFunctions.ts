export interface PositionMarker {
    pos: number;
    len: number;
}

export type SearchMatchFunction = (lhs: string, rhs: string, markers: PositionMarker[]) => boolean;

function toLowerCaseDeaccented(str: string): string {
    return str.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
}

function indexedFuzzySearchMatchFunction(lhs: string, lhsIndex: number, rhs: string, markers: PositionMarker[]): boolean {
    if (!rhs) {
        return true;
    }

    lhs = toLowerCaseDeaccented(lhs);
    rhs = toLowerCaseDeaccented(rhs);

    for (let len = rhs.length; len >= 1; len -= 1) {
        const subWord = rhs.substring(0, len);
        const foundPos = lhs.indexOf(subWord, lhsIndex);

        if (foundPos >= 0) {
            markers.push({
                pos: foundPos,
                len: subWord.length
            });

            return indexedFuzzySearchMatchFunction(lhs, foundPos + subWord.length, rhs.substring(len), markers);
        }
    }

    return false;
}

export function fuzzySearchMatchFunction(lhs: string, rhs: string, markers: PositionMarker[]): boolean {
    return indexedFuzzySearchMatchFunction(lhs, 0, rhs, markers);
}

export function containsSearchMatchFunction(lhs: string, rhs: string, markers: PositionMarker[]): boolean {
    const index = toLowerCaseDeaccented(lhs).indexOf(toLowerCaseDeaccented(rhs));

    if (index < 0) {
        return false;
    }

    markers.push({
        pos: index,
        len: rhs.length
    });

    return true;
}

export function exactSearchMatchFunction(lhs: string, rhs: string, markers: PositionMarker[]): boolean {
    if (toLowerCaseDeaccented(lhs) === toLowerCaseDeaccented(rhs)) {
        markers.push({
            pos: 0,
            len: rhs.length
        });
        return true;
    }

    return false;
}
