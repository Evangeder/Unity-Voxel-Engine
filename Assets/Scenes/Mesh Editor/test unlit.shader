// Vertex-colored unlit shader v1.0

// Author: Bishop Myers

// Last modified: October 7, 2014

Shader "Custom/VertexColorUnlit" {

	Properties{

	}



	Category{

		Tags { "Queue" = "Geometry" }

		Lighting Off

		BindChannels {

			Bind "Color", color

			Bind "Vertex", vertex

			Bind "TexCoord", texcoord

		}



		SubShader {

			Pass {

			}

		}

	}

}