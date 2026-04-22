# Contracts: Destination Reviews API

Base route: `/api/destinations`
Auth: JWT bearer required for create/update/delete operations.

## 1) Get destination reviews (cursor pagination)
`GET /api/destinations/{destinationId:int}/reviews?cursor={cursor?}&pageSize={pageSize?}`

### Response 200
```json
{
  "destinationId": 123,
  "averageRating": 4.3,
  "totalReviews": 241,
  "isEmpty": false,
  "hasMore": true,
  "nextCursor": "2026-04-19T16:20:44.321Z|9821",
  "items": [
	{
	  "reviewId": 9821,
	  "reviewerName": "Sara M.",
	  "reviewDateUtc": "2026-04-19T16:20:44.321Z",
	  "rating": 5,
	  "comment": "Amazing views and friendly locals.",
	  "isMine": false
	}
  ]
}
```

### Empty-state example
```json
{
  "destinationId": 123,
  "averageRating": 0.0,
  "totalReviews": 0,
  "isEmpty": true,
  "hasMore": false,
  "nextCursor": null,
  "items": []
}
```

### Errors
- `404` destination not found
- `400` invalid cursor or page size

---

## 2) Upsert my review for a destination
`POST /api/destinations/{destinationId:int}/reviews`

### Request body
```json
{
  "rating": 4,
  "comment": "Great place for a weekend trip"
}
```

### Rules
- If active review for current user exists: update it.
- Else: create active review.

### Response 200
```json
{
  "reviewId": 9821,
  "destinationId": 123,
  "rating": 4,
  "comment": "Great place for a weekend trip",
  "updatedAtUtc": "2026-04-19T16:25:10.100Z",
  "averageRating": 4.2,
  "totalReviews": 242
}
```

### Errors
- `401` unauthorized
- `404` destination not found
- `400` validation errors (`rating` out of range, comment too long)

---

## 3) Update my existing review explicitly
`PUT /api/destinations/{destinationId:int}/reviews/me`

### Request body
```json
{
  "rating": 3,
  "comment": "Updated feedback after second visit"
}
```

### Response 200
Same shape as upsert response.

### Errors
- `401` unauthorized
- `404` destination or active user review not found
- `400` validation errors

---

## 4) Soft delete my review
`DELETE /api/destinations/{destinationId:int}/reviews/me`

### Behavior
- Mark active review as `IsActive=false`
- Recalculate destination aggregate

### Response 200
```json
{
  "destinationId": 123,
  "deleted": true,
  "averageRating": 4.1,
  "totalReviews": 241
}
```

### Errors
- `401` unauthorized
- `404` destination or active user review not found

---

## Validation Contract
- `rating`: integer in `[1..5]`
- `comment`: optional, max 500 chars, whitespace-only treated as null

## Ordering Contract
- Review list order: newest first by `CreatedAtUtc`, then `ReviewId` descending

## Concurrency Contract
- Last-write-wins for the same user+destination review, based on server processing order

## Aggregate Contract
- Aggregates (`averageRating`, `totalReviews`) include active reviews only
- `averageRating` is returned rounded to 1 decimal place
