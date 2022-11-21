export class RunInfo {
	lastRun: string;
	nextRun: string;

	constructor(lastRun: string, nextRun: string) {
		this.lastRun = lastRun;
		this.nextRun = nextRun;
	}
}