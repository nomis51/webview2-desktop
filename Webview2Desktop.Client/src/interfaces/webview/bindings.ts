export interface Query {
	randomNumber?: {
		min: number
	},
	randomNumber2?: {},
}

export function createQuery(query: Query): string | undefined {
	return Object.keys(query).pop();
}