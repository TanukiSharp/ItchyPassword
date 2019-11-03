import { QRCodeBits, QRCodeWriter } from './qrcode';

function main() {
    const svgOutput: HTMLElement | null = document.getElementById('svgOutput');
    if (svgOutput === null) {
        return;
    }

    const content: QRCodeBits = {
        width: 8,
        height: 8,
        bits: [
            [0,0,0,0,0,0,0,0],
            [0,0,1,0,1,0,1,0],
            [0,1,0,1,0,1,0,0],
            [0,0,1,0,0,0,1,0],
            [0,1,0,0,0,1,0,0],
            [0,0,1,0,1,0,1,0],
            [0,1,0,1,0,1,0,0],
            [0,0,0,0,0,0,0,0]
        ]
    }

    QRCodeWriter.write(content, svgOutput);
}

main();
