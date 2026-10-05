import { resolve } from 'path'
import { defineConfig } from 'vite'

export default defineConfig({
	build:{
		outDir: "..\\wwwroot",
		emptyOutDir: true,
		rolldownOptions: {
			input: {
				main: resolve(__dirname, 'index.html'),
				settings: resolve(__dirname, 'settings.html')
			}
		}
	}
})
