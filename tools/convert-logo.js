const sharp = require('sharp');
const path = require('path');
const fs = require('fs');

/**
 * Script para converter imagens de logo
 * Uso: node convert-logo.js <caminho-da-imagem>
 */

async function convertLogo(imagePath) {
  try {
    const fileName = path.basename(imagePath, path.extname(imagePath));
    const outputDir = path.join(__dirname, '..', 'public', 'assets');

    if (!fs.existsSync(outputDir)) {
      fs.mkdirSync(outputDir, { recursive: true });
    }

    // Converter para PNG
    await sharp(imagePath)
      .png({ quality: 80 })
      .toFile(path.join(outputDir, `${fileName}.png`));

    console.log(`✓ Logo convertida para PNG: ${fileName}.png`);

    // Converter para WebP
    await sharp(imagePath)
      .webp({ quality: 80 })
      .toFile(path.join(outputDir, `${fileName}.webp`));

    console.log(`✓ Logo convertida para WebP: ${fileName}.webp`);

  } catch (error) {
    console.error('Erro ao converter logo:', error.message);
    process.exit(1);
  }
}

const logoPath = process.argv[2];
if (!logoPath) {
  console.error('Por favor, forneça o caminho da imagem.');
  process.exit(1);
}

convertLogo(logoPath);
