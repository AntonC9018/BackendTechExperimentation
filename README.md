The user type ends up in the schema, even though none of the endpoints reference it.

```json
{
  "openapi": "3.0.1",
  "info": {
    "title": "App",
    "version": "1.0"
  },
  "paths": {
    "/test": {
      "get": {
        "tags": [
          "Test"
        ],
        "responses": {
          "200": {
            "description": "Success",
            "content": {
              "text/plain": {
                "schema": {
                  "$ref": "#/components/schemas/UserDto"
                }
              },
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/UserDto"
                }
              },
              "text/json": {
                "schema": {
                  "$ref": "#/components/schemas/UserDto"
                }
              }
            }
          }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "User": {
        "type": "object",
        "properties": {
          "name": {
            "type": "string",
            "nullable": true
          }
        },
        "additionalProperties": false
      },
      "UserDto": {
        "type": "object",
        "properties": {
          "name": {
            "type": "string",
            "nullable": true
          }
        },
        "additionalProperties": false
      }
    }
  }
}
```