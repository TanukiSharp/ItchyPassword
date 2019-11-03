export interface QRCodeBits {
    width: number;
    height: number;
    bits: (boolean|number)[][];
}

export class QRCodeWriter {
    static CELL_WIDTH: number = 32;
    static CELL_HEIGHT: number = 32;

    static svgns = 'http://www.w3.org/2000/svg';

    static write(content: QRCodeBits, output: HTMLElement) {
        const offsetX: number = 0;
        const offsetY: number = 0;
        const width: number = content.width;
        const height: number = content.height;

        output.setAttribute('width', (width * QRCodeWriter.CELL_WIDTH).toString());
        output.setAttribute('height', (height * QRCodeWriter.CELL_HEIGHT).toString());

        for (let x = offsetX; x < width; x += 1) {
            for (let y = offsetY; y < height; y += 1) {
                const value = content.bits[y][x];

                const shape = document.createElementNS(QRCodeWriter.svgns, 'rect');
                // The +0.5 below are here to avoid sub-pixel glitches when zooming.
                shape.setAttributeNS(null, 'x', (x * QRCodeWriter.CELL_WIDTH + 0.5).toString());
                shape.setAttributeNS(null, 'y', (y * QRCodeWriter.CELL_HEIGHT + 0.5).toString());
                shape.setAttributeNS(null, 'width',  (QRCodeWriter.CELL_WIDTH + 0.5).toString());
                shape.setAttributeNS(null, 'height', (QRCodeWriter.CELL_HEIGHT + 0.5).toString());
                shape.setAttributeNS(null, 'fill', value ? 'white' : 'black');

                output.appendChild(shape);
            }
        }
    }
}
