import { resolve } from 'path'
import { defineConfig } from 'vite'

export default defineConfig({
	build:{
		outDir: "..\\wwwroot",
		emptyOutDir: true,
		rollupOptions: {
			input: {
				main: resolve(__dirname, 'index.html'),
				settings: resolve(__dirname, 'settings.html')
			}
		}
	}
})
