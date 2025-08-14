import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { parseStringPromise } from 'xml2js';

// Needed to simulate __dirname in ESM
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const licensePath = path.resolve(__dirname, '../../License.lic');
const envPath = path.resolve(__dirname, '../.env');

async function updateEnvFromLicense() {
    try {
        if (!fs.existsSync(licensePath)) {
            console.log('License.lic not found. Skipping update.');
            return;
        }

        const xmlData = fs.readFileSync(licensePath, 'utf-8');
        const parsedXml = await parseStringPromise(xmlData);

        const id = parsedXml?.License?.Data?.[0].Id?.[0];

        if (!id) {
            console.warn('ID not found in License.lic. Skipping update.');
            return;
        }

        let envContent = '';
        if (fs.existsSync(envPath)) {
            envContent = fs.readFileSync(envPath, 'utf-8');
            const regex = /^VITE_LICENSE_ID=.*$/m;
            if (regex.test(envContent)) {
                envContent = envContent.replace(regex, `VITE_LICENSE_ID=${id}`);
            } else {
                envContent += `\nVITE_LICENSE_ID=${id}`;
            }
        } else {
            envContent = `VITE_LICENSE_ID=${id}`;
        }

        fs.writeFileSync(envPath, envContent);
        console.log('Updated .env with VITE_LICENSE_ID');
    } catch (error) {
        console.error('Error updating .env:', error.message);
    }
}

updateEnvFromLicense();
